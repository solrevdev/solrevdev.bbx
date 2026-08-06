using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Pipelines.AddDeploymentVariable;
using Bbx.Features.Pipelines.ChangeDeploymentEnvironment;
using Bbx.Features.Pipelines.CreateDeploymentEnvironment;
using Bbx.Features.Pipelines.CreateReportAnnotations;
using Bbx.Features.Pipelines.DeleteDeploymentEnvironment;
using Bbx.Features.Pipelines.DeleteDeploymentVariable;
using Bbx.Features.Pipelines.DeletePipelineReport;
using Bbx.Features.Pipelines.DeleteReportAnnotation;
using Bbx.Features.Pipelines.ListDeployments;
using Bbx.Features.Pipelines.ListDeploymentVariables;
using Bbx.Features.Pipelines.UpdateDeploymentVariable;
using Bbx.Features.Pipelines.UpsertPipelineReport;
using Bbx.Features.Pipelines.UpsertReportAnnotation;
using Bbx.Features.Pipelines.ViewDeployment;
using Bbx.Features.Pipelines.ViewReportAnnotation;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Pipelines;

/// <summary>
/// Deployment environments and variables, deployment records, and the report
/// and annotation writes CI uses to publish its findings.
/// </summary>
public class DeploymentAndReportHandlerTests
{
    private const string Hash = "abc123";

    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    private static JsonElement Body(FakeHttpMessageHandler http)
        => JsonDocument.Parse(http.CallBodies[0]!).RootElement;

    // Bitbucket refuses an environment body without a rank: it orders the
    // environments in the deployment view.
    [Fact]
    public async Task Environment_create_sends_the_type_and_the_rank()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"{e}","name":"Staging"}""");

        await new CreateDeploymentEnvironmentHandler(Client(http), Creds()).HandleAsync(
            new CreateDeploymentEnvironmentRequest("ws", "repo", "Staging", "Staging", 1),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Post);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/environments");
        var body = Body(http);
        body.GetProperty("type").GetString().Should().Be("deployment_environment");
        body.GetProperty("environment_type").GetProperty("name").GetString().Should().Be("Staging");
        body.GetProperty("environment_type").GetProperty("rank").GetInt32().Should().Be(1);
        body.GetProperty("rank").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task An_unknown_environment_type_fails_before_calling_the_api()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new CreateDeploymentEnvironmentHandler(Client(http), Creds()).HandleAsync(
            new CreateDeploymentEnvironmentRequest("ws", "repo", "Staging", "QA", 1),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Environment_delete_deletes_it()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");

        await new DeleteDeploymentEnvironmentHandler(Client(http), Creds()).HandleAsync(
            new DeleteDeploymentEnvironmentRequest("ws", "repo", "{e}"),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Delete);
        call.RequestUri!.AbsoluteUri.Should()
            .Be("https://api.bitbucket.org/2.0/repositories/ws/repo/environments/%7Be%7D");
    }

    // Environments are changed through a changes endpoint rather than a PUT on
    // the environment itself, and the body is a change envelope. The spec
    // documents no body at all, so this shape was read off the Bitbucket web
    // UI, which posts exactly this, trailing slash included. Confirmed live on
    // 2026-08-06: it answers 202 with an empty body.
    [Fact]
    public async Task Environment_changes_post_a_change_envelope()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Accepted, "");

        await new ChangeDeploymentEnvironmentHandler(Client(http), Creds()).HandleAsync(
            new ChangeDeploymentEnvironmentRequest("ws", "repo", "e1", "Prod", true),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Post);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/environments/e1/changes/");
        var change = Body(http).GetProperty("change");
        change.GetProperty("name").GetString().Should().Be("Prod");
        change.GetProperty("restrictions").GetProperty("admin_only").GetBoolean().Should().BeTrue();
    }

    // Only name and restrictions can change. Everything else, the lock
    // included, is answered with 400 change-not-supported, so nothing else is
    // offered.
    [Fact]
    public async Task Clearing_the_restriction_sends_admin_only_false()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Accepted, "");

        await new ChangeDeploymentEnvironmentHandler(Client(http), Creds()).HandleAsync(
            new ChangeDeploymentEnvironmentRequest("ws", "repo", "e1", null, false),
            TestContext.Current.CancellationToken);

        var change = Body(http).GetProperty("change");
        change.TryGetProperty("name", out _).Should().BeFalse();
        change.GetProperty("restrictions").GetProperty("admin_only").GetBoolean().Should().BeFalse();
    }

    // An empty change envelope is answered with a bare 400, so the handler
    // refuses before the call rather than passing one on.
    [Fact]
    public async Task An_empty_change_is_refused_before_the_call()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new ChangeDeploymentEnvironmentHandler(Client(http), Creds()).HandleAsync(
            new ChangeDeploymentEnvironmentRequest("ws", "repo", "e1", null, null),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(ChangeDeploymentEnvironmentHandler.NothingToChange);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Deployment_variables_live_under_deployments_config()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await new ListDeploymentVariablesHandler(Client(http), Creds()).HandleAsync(
            new ListDeploymentVariablesRequest("ws", "repo", "e1", 50),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/deployments_config/environments/e1/variables");
    }

    [Fact]
    public async Task Deployment_variable_add_uses_the_deployment_discriminator()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"v1","key":"K","value":"v"}""");

        await new AddDeploymentVariableHandler(Client(http), Creds()).HandleAsync(
            new AddDeploymentVariableRequest("ws", "repo", "e1", "K", "v", false),
            TestContext.Current.CancellationToken);

        Body(http).GetProperty("type").GetString().Should().Be("deployment_variable");
    }

    [Fact]
    public async Task Deployment_variable_update_targets_the_variable()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"v1","key":"K","value":"v2"}""");

        await new UpdateDeploymentVariableHandler(Client(http), Creds()).HandleAsync(
            new UpdateDeploymentVariableRequest("ws", "repo", "e1", "v1", null, "v2", null),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Put);
        call.RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/deployments_config/environments/e1/variables/v1");
    }

    [Fact]
    public async Task Deployment_variable_update_with_nothing_set_fails()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new UpdateDeploymentVariableHandler(Client(http), Creds()).HandleAsync(
            new UpdateDeploymentVariableRequest("ws", "repo", "e1", "v1", null, null, null),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(UpdateDeploymentVariableHandler.NothingToUpdate);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Deployment_variable_delete_deletes_it()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");

        await new DeleteDeploymentVariableHandler(Client(http), Creds()).HandleAsync(
            new DeleteDeploymentVariableRequest("ws", "repo", "e1", "v1"),
            TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Delete);
    }

    // Deployments are a different resource from the environments they target.
    [Fact]
    public async Task Deploys_list_reads_the_deployments_collection()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[{"uuid":"d1","environment":{"name":"Staging"},"state":{"name":"COMPLETED"},
             "release":{"created_on":"2026-01-01","commit":{"hash":"abc"}}}],"next":null}
            """);

        var result = (dynamic)await new ListDeploymentsHandler(Client(http), Creds()).HandleAsync(
            new ListDeploymentsRequest("ws", "repo", 25), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/deployments");
        ((int)result.count).Should().Be(1);
        ((string)result.deployments[0].commit).Should().Be("abc");
    }

    [Fact]
    public async Task Deploys_view_reads_one_deployment()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"d1"}""");

        await new ViewDeploymentHandler(Client(http), Creds()).HandleAsync(
            new ViewDeploymentRequest("ws", "repo", "d1"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/deployments/d1");
    }

    // The report ID is chosen by the caller, so the PUT creates as well as
    // updates. That is what makes it usable from a CI step with no memory.
    [Fact]
    public async Task Report_update_puts_to_the_caller_chosen_id()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"r1","title":"Coverage"}""");

        await new UpsertPipelineReportHandler(Client(http), Creds()).HandleAsync(
            new UpsertPipelineReportRequest("ws", "repo", Hash, "coverage-1", "Coverage",
                "Line coverage for the build", "coverage", "passed", null),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Put);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/commit/abc123/reports/coverage-1");
        var body = Body(http);
        body.GetProperty("report_type").GetString().Should().Be("COVERAGE");
        body.GetProperty("result").GetString().Should().Be("PASSED");
    }

    // The spec marks nothing required, but Bitbucket refuses a report without
    // details: "Cannot build Report, some of required attributes are not set
    // [details]". Verified live on 2026-08-05, which is why --details is a
    // required option rather than an optional one.
    [Fact]
    public async Task Report_update_always_sends_details_and_the_type_discriminator()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"r1"}""");

        await new UpsertPipelineReportHandler(Client(http), Creds()).HandleAsync(
            new UpsertPipelineReportRequest("ws", "repo", Hash, "r1", "T", "why it exists", null, null, null),
            TestContext.Current.CancellationToken);

        var body = Body(http);
        body.GetProperty("type").GetString().Should().Be("report");
        body.GetProperty("details").GetString().Should().Be("why it exists");
    }

    [Fact]
    public async Task Annotation_update_sends_the_type_discriminator()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"a1"}""");

        await new UpsertReportAnnotationHandler(Client(http), Creds()).HandleAsync(
            new UpsertReportAnnotationRequest("ws", "repo", Hash, "r1", "a1", "s",
                null, null, null, null, null),
            TestContext.Current.CancellationToken);

        Body(http).GetProperty("type").GetString().Should().Be("report_annotation");
    }

    [Fact]
    public async Task Report_update_defaults_the_type_to_test()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"r1"}""");

        await new UpsertPipelineReportHandler(Client(http), Creds()).HandleAsync(
            new UpsertPipelineReportRequest("ws", "repo", Hash, "r1", "T", "d", null, null, null),
            TestContext.Current.CancellationToken);

        Body(http).GetProperty("report_type").GetString().Should().Be("TEST");
    }

    [Theory]
    [InlineData("SPELLING", null)]
    [InlineData(null, "MAYBE")]
    public async Task An_unknown_report_type_or_result_fails_before_calling_the_api(string? type, string? result)
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new UpsertPipelineReportHandler(Client(http), Creds()).HandleAsync(
            new UpsertPipelineReportRequest("ws", "repo", Hash, "r1", "T", "d", type, result, null),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Report_delete_deletes_it()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");

        await new DeletePipelineReportHandler(Client(http), Creds()).HandleAsync(
            new DeletePipelineReportRequest("ws", "repo", Hash, "r1"),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Delete);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/commit/abc123/reports/r1");
    }

    // This endpoint takes a bare array rather than an object, which is unusual
    // enough that wrapping it would be the natural mistake.
    [Fact]
    public async Task Annotations_create_posts_the_array_unwrapped()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """[{"uuid":"a1"}]""");

        await new CreateReportAnnotationsHandler(Client(http), Creds()).HandleAsync(
            new CreateReportAnnotationsRequest("ws", "repo", Hash, "r1",
                """[{"external_id":"a1","summary":"Unused import"}]"""),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Post);
        call.RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/commit/abc123/reports/r1/annotations");
        Body(http).ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task Annotations_create_refuses_json_that_is_not_an_array()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new CreateReportAnnotationsHandler(Client(http), Creds()).HandleAsync(
            new CreateReportAnnotationsRequest("ws", "repo", Hash, "r1", """{"summary":"x"}"""),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(CreateReportAnnotationsHandler.NotAnArray);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Annotations_create_refuses_invalid_json()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new CreateReportAnnotationsHandler(Client(http), Creds()).HandleAsync(
            new CreateReportAnnotationsRequest("ws", "repo", Hash, "r1", "not json"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Annotation_view_reads_the_single_annotation()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"a1"}""");

        await new ViewReportAnnotationHandler(Client(http), Creds()).HandleAsync(
            new ViewReportAnnotationRequest("ws", "repo", Hash, "r1", "a1"),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/commit/abc123/reports/r1/annotations/a1");
    }

    [Fact]
    public async Task Annotation_update_upper_cases_the_type_and_severity()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"a1"}""");

        await new UpsertReportAnnotationHandler(Client(http), Creds()).HandleAsync(
            new UpsertReportAnnotationRequest("ws", "repo", Hash, "r1", "a1", "Unused import",
                null, "code_smell", "low", "src/a.cs", 3),
            TestContext.Current.CancellationToken);

        var body = Body(http);
        body.GetProperty("annotation_type").GetString().Should().Be("CODE_SMELL");
        body.GetProperty("severity").GetString().Should().Be("LOW");
        body.GetProperty("path").GetString().Should().Be("src/a.cs");
        body.GetProperty("line").GetInt32().Should().Be(3);
    }

    // A line with no file anchors to nothing, the same trap as a commit
    // comment.
    [Fact]
    public async Task An_annotation_line_without_a_path_fails_before_calling_the_api()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new UpsertReportAnnotationHandler(Client(http), Creds()).HandleAsync(
            new UpsertReportAnnotationRequest("ws", "repo", Hash, "r1", "a1", "s",
                null, null, null, null, 3),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(UpsertReportAnnotationHandler.LineNeedsPath);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Annotation_delete_deletes_it()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");

        await new DeleteReportAnnotationHandler(Client(http), Creds()).HandleAsync(
            new DeleteReportAnnotationRequest("ws", "repo", Hash, "r1", "a1"),
            TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Delete);
    }
}
