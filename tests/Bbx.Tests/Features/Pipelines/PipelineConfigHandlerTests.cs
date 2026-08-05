using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Pipelines.AddPipelineKnownHost;
using Bbx.Features.Pipelines.ClearAllPipelineCaches;
using Bbx.Features.Pipelines.DeletePipelineSshKeyPair;
using Bbx.Features.Pipelines.ListPipelineScheduleExecutions;
using Bbx.Features.Pipelines.ListTestCaseReasons;
using Bbx.Features.Pipelines.PipelineCacheContentUri;
using Bbx.Features.Pipelines.PipelineLogs;
using Bbx.Features.Pipelines.SetPipelineBuildNumber;
using Bbx.Features.Pipelines.SetPipelineSshKeyPair;
using Bbx.Features.Pipelines.UpdatePipelineKnownHost;
using Bbx.Features.Pipelines.UpdatePipelineSchedule;
using Bbx.Features.Pipelines.UpdatePipelinesConfig;
using Bbx.Features.Pipelines.UpdatePipelineVariable;
using Bbx.Features.Pipelines.ViewPipelineSchedule;
using Bbx.Features.Pipelines.ViewPipelinesConfig;
using Bbx.Features.Pipelines.ViewPipelineSshKeyPair;
using Bbx.Features.Pipelines.ViewPipelineStep;
using Bbx.Features.Pipelines.ViewPipelineVariable;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Pipelines;

/// <summary>
/// Pipeline configuration, schedules, variables, SSH and caches.
/// </summary>
public class PipelineConfigHandlerTests
{
    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    private static JsonElement Body(FakeHttpMessageHandler http)
        => JsonDocument.Parse(http.CallBodies[0]!).RootElement;

    [Fact]
    public async Task Config_view_gets_the_pipelines_config()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"enabled":true}""");

        await new ViewPipelinesConfigHandler(Client(http), Creds()).HandleAsync(
            new ViewPipelinesConfigRequest("ws", "repo"), TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Get);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/pipelines_config");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Config_update_puts_the_enabled_flag(bool enabled)
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"enabled":true}""");

        await new UpdatePipelinesConfigHandler(Client(http), Creds()).HandleAsync(
            new UpdatePipelinesConfigRequest("ws", "repo", enabled), TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Put);
        Body(http).GetProperty("enabled").GetBoolean().Should().Be(enabled);
    }

    [Fact]
    public async Task Build_number_puts_the_next_value()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"next":500}""");

        await new SetPipelineBuildNumberHandler(Client(http), Creds()).HandleAsync(
            new SetPipelineBuildNumberRequest("ws", "repo", 500), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pipelines_config/build_number");
        Body(http).GetProperty("next").GetInt32().Should().Be(500);
    }

    [Fact]
    public async Task A_build_number_below_one_fails_before_calling_the_api()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new SetPipelineBuildNumberHandler(Client(http), Creds()).HandleAsync(
            new SetPipelineBuildNumberRequest("ws", "repo", 0), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(SetPipelineBuildNumberHandler.MustBeAhead);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Schedule_view_gets_the_single_schedule()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"{s}"}""");

        await new ViewPipelineScheduleHandler(Client(http), Creds()).HandleAsync(
            new ViewPipelineScheduleRequest("ws", "repo", "{s}"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri.Should()
            .Be("https://api.bitbucket.org/2.0/repositories/ws/repo/pipelines_config/schedules/%7Bs%7D");
    }

    [Fact]
    public async Task Schedule_update_sends_only_what_changed()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"{s}"}""");

        await new UpdatePipelineScheduleHandler(Client(http), Creds()).HandleAsync(
            new UpdatePipelineScheduleRequest("ws", "repo", "{s}", false, null),
            TestContext.Current.CancellationToken);

        var body = Body(http);
        body.GetProperty("enabled").GetBoolean().Should().BeFalse();
        body.TryGetProperty("cron_pattern", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Schedule_update_with_nothing_set_fails_before_calling_the_api()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new UpdatePipelineScheduleHandler(Client(http), Creds()).HandleAsync(
            new UpdatePipelineScheduleRequest("ws", "repo", "{s}", null, null),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(UpdatePipelineScheduleHandler.NothingToUpdate);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Schedule_executions_reads_the_executions_collection()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[{"uuid":"e1"}],"next":null}""");

        var result = (dynamic)await new ListPipelineScheduleExecutionsHandler(Client(http), Creds())
            .HandleAsync(new ListPipelineScheduleExecutionsRequest("ws", "repo", "s1", 25),
                TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pipelines_config/schedules/s1/executions");
        ((int)result.count).Should().Be(1);
    }

    // A secured variable never comes back with its value. Reporting an empty
    // string would read as "the value is blank" rather than "it is hidden".
    [Fact]
    public async Task A_secured_variable_reads_back_masked()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"v1","key":"TOKEN","secured":true}""");

        var result = (dynamic)await new ViewPipelineVariableHandler(Client(http), Creds()).HandleAsync(
            new ViewPipelineVariableRequest("ws", "repo", "v1"), TestContext.Current.CancellationToken);

        ((string)result.value).Should().Be("***");
        ((bool)result.secured).Should().BeTrue();
    }

    // The type discriminator is always sent, so the "did the caller change
    // anything" check has to look past it.
    [Fact]
    public async Task Variable_update_with_nothing_but_the_discriminator_fails()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new UpdatePipelineVariableHandler(Client(http), Creds()).HandleAsync(
            new UpdatePipelineVariableRequest("ws", "repo", "v1", null, null, null),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(UpdatePipelineVariableHandler.NothingToUpdate);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Variable_update_keeps_the_type_discriminator()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"v1","key":"K","value":"x"}""");

        await new UpdatePipelineVariableHandler(Client(http), Creds()).HandleAsync(
            new UpdatePipelineVariableRequest("ws", "repo", "v1", null, "x", null),
            TestContext.Current.CancellationToken);

        var body = Body(http);
        body.GetProperty("type").GetString().Should().Be("pipeline_variable");
        body.GetProperty("value").GetString().Should().Be("x");
    }

    // The private half is write-only. Bitbucket never returns it and the
    // handler does not go looking.
    [Fact]
    public async Task Key_pair_view_reports_only_the_public_half()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"type":"pipeline_ssh_key_pair","public_key":"ssh-rsa AAAA"}""");

        var result = (dynamic)await new ViewPipelineSshKeyPairHandler(Client(http), Creds()).HandleAsync(
            new ViewPipelineSshKeyPairRequest("ws", "repo"), TestContext.Current.CancellationToken);

        ((string)result.public_key).Should().Be("ssh-rsa AAAA");
    }

    [Fact]
    public async Task Key_pair_set_puts_both_halves()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"public_key":"ssh-rsa AAAA"}""");

        await new SetPipelineSshKeyPairHandler(Client(http), Creds()).HandleAsync(
            new SetPipelineSshKeyPairRequest("ws", "repo", "PRIVATE", "ssh-rsa AAAA"),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Put);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/pipelines_config/ssh/key_pair");
        var body = Body(http);
        body.GetProperty("private_key").GetString().Should().Be("PRIVATE");
        body.GetProperty("public_key").GetString().Should().Be("ssh-rsa AAAA");
    }

    [Fact]
    public async Task Key_pair_delete_deletes_it()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");

        await new DeletePipelineSshKeyPairHandler(Client(http), Creds()).HandleAsync(
            new DeletePipelineSshKeyPairRequest("ws", "repo"), TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Delete);
    }

    // Bitbucket refuses a known-host body without the two type discriminators
    // with "An invalid field was found in the JSON payload".
    [Fact]
    public async Task Known_host_add_carries_both_type_discriminators()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"h1","hostname":"bitbucket.org"}""");

        await new AddPipelineKnownHostHandler(Client(http), Creds()).HandleAsync(
            new AddPipelineKnownHostRequest("ws", "repo", "bitbucket.org", "ssh-rsa", "AAAA"),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Post);
        call.RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pipelines_config/ssh/known_hosts");
        var body = Body(http);
        body.GetProperty("type").GetString().Should().Be("pipeline_known_host");
        body.GetProperty("public_key").GetProperty("type").GetString().Should().Be("pipeline_ssh_public_key");
        body.GetProperty("public_key").GetProperty("key_type").GetString().Should().Be("ssh-rsa");
    }

    [Fact]
    public async Task Known_host_update_replaces_the_whole_host()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"h1"}""");

        await new UpdatePipelineKnownHostHandler(Client(http), Creds()).HandleAsync(
            new UpdatePipelineKnownHostRequest("ws", "repo", "h1", "example.com", "ssh-ed25519", "BBBB"),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Put);
        call.RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pipelines_config/ssh/known_hosts/h1");
        Body(http).GetProperty("public_key").GetProperty("key").GetString().Should().Be("BBBB");
    }

    // Clearing every cache is its own endpoint, with no cache name in the path.
    [Fact]
    public async Task Clear_all_caches_deletes_the_collection()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");

        await new ClearAllPipelineCachesHandler(Client(http), Creds()).HandleAsync(
            new ClearAllPipelineCachesRequest("ws", "repo"), TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Delete);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/pipelines-config/caches");
    }

    [Fact]
    public async Task Cache_content_uri_reads_the_signed_url_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uri":"https://example.com/cache.tar"}""");

        await new PipelineCacheContentUriHandler(Client(http), Creds()).HandleAsync(
            new PipelineCacheContentUriRequest("ws", "repo", "c1"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pipelines-config/caches/c1/content-uri");
    }

    [Fact]
    public async Task Step_view_gets_the_single_step()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"{st}","name":"Build","state":{"name":"COMPLETED"}}""");

        await new ViewPipelineStepHandler(Client(http), Creds()).HandleAsync(
            new ViewPipelineStepRequest("ws", "repo", "{p}", "{st}"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri.Should()
            .Be("https://api.bitbucket.org/2.0/repositories/ws/repo/pipelines/%7Bp%7D/steps/%7Bst%7D");
    }

    // The numbered logs live under a different path from the step's own log,
    // not behind a query parameter on it.
    [Fact]
    public async Task Logs_without_a_log_uuid_read_the_whole_step()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, "output", "text/plain");

        await new PipelineLogsHandler(Client(http), Creds()).HandleAsync(
            new PipelineLogsRequest("ws", "repo", "p1", "s1"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pipelines/p1/steps/s1/log");
    }

    [Fact]
    public async Task Logs_with_a_log_uuid_read_that_numbered_log()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, "output", "text/plain");

        await new PipelineLogsHandler(Client(http), Creds()).HandleAsync(
            new PipelineLogsRequest("ws", "repo", "p1", "s1", "l2"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pipelines/p1/steps/s1/logs/l2");
    }

    [Fact]
    public async Task Test_case_reasons_reads_the_nested_collection()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[{"message":"expected 1"}],"next":null}""");

        await new ListTestCaseReasonsHandler(Client(http), Creds()).HandleAsync(
            new ListTestCaseReasonsRequest("ws", "repo", "p1", "s1", "tc1", 50),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath.Should()
            .Be("/2.0/repositories/ws/repo/pipelines/p1/steps/s1/test_reports/test_cases/tc1/test_case_reasons");
    }

    [Fact]
    public async Task A_missing_repository_fails_before_any_call_goes_out()
    {
        var http = new FakeHttpMessageHandler();
        var creds = new CredentialManager(new InMemoryCredentialStore());

        var act = async () => await new ViewPipelinesConfigHandler(Client(http), creds).HandleAsync(
            new ViewPipelinesConfigRequest(null, null), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }
}
