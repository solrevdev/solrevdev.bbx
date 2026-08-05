using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.BranchingModel.EffectiveBranchingModel;
using Bbx.Features.Repos.DefaultReviewers.ViewDefaultReviewer;
using Bbx.Features.Repos.DeployKeys.UpdateRepoDeployKey;
using Bbx.Features.Repos.FileConflicts;
using Bbx.Features.Repos.OverrideSettings.UpdateOverrideSettings;
using Bbx.Features.Repos.OverrideSettings.ViewOverrideSettings;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Repos;

/// <summary>
/// The read-and-update halves added to groups that previously only created,
/// listed or deleted.
/// </summary>
public class RepoSettingsHandlerTests
{
    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    private static CredentialManager NoWorkspace() => new(new InMemoryCredentialStore());

    private static JsonElement Body(FakeHttpMessageHandler http)
        => JsonDocument.Parse(http.CallBodies[0]!).RootElement;

    [Fact]
    public async Task Override_settings_view_gets_the_override_settings_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"default_reviewers":true}""");

        await new ViewOverrideSettingsHandler(Client(http), Creds())
            .HandleAsync(new ViewOverrideSettingsRequest("ws", "repo"), TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Get);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/override-settings");
    }

    // Each flag decides whether the repository overrides its project or
    // inherits from it, so sending one the caller did not mention would
    // silently re-inherit a setting they had overridden.
    [Fact]
    public async Task Override_settings_update_sends_only_the_flags_it_was_given()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");
        http.Enqueue(HttpStatusCode.OK, """{"branching_model":false}""");

        await new UpdateOverrideSettingsHandler(Client(http), Creds()).HandleAsync(
            new UpdateOverrideSettingsRequest("ws", "repo", null, false, null),
            TestContext.Current.CancellationToken);

        http.Calls[0].Method.Should().Be(HttpMethod.Put);
        var body = Body(http);
        body.GetProperty("branching_model").GetBoolean().Should().BeFalse();
        body.TryGetProperty("default_reviewers", out _).Should().BeFalse();
        body.TryGetProperty("branch_restrictions", out _).Should().BeFalse();
    }

    // The PUT answers 204 with no body, so there is nothing to report from it.
    // Reading the settings back gives the caller the state they just set
    // instead of an empty JsonElement, which the serializer throws on.
    // Verified against the live API on 2026-08-05.
    [Fact]
    public async Task Override_settings_update_reads_the_settings_back()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");
        http.Enqueue(HttpStatusCode.OK, """{"default_merge_strategy":true,"branching_model":true}""");

        var result = await new UpdateOverrideSettingsHandler(Client(http), Creds()).HandleAsync(
            new UpdateOverrideSettingsRequest("ws", "repo", null, true, null),
            TestContext.Current.CancellationToken);

        http.Calls.Should().HaveCount(2);
        http.Calls[1].Method.Should().Be(HttpMethod.Get);
        http.Calls[1].RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/override-settings");
        result.GetProperty("branching_model").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Override_settings_update_with_no_flags_fails_before_calling_the_api()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new UpdateOverrideSettingsHandler(Client(http), Creds()).HandleAsync(
            new UpdateOverrideSettingsRequest("ws", "repo", null, null, null),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(UpdateOverrideSettingsHandler.NothingToUpdate);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Effective_branching_model_gets_its_own_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"development":{"name":"main"}}""");

        await new EffectiveBranchingModelHandler(Client(http), Creds()).HandleAsync(
            new EffectiveBranchingModelRequest("ws", "repo"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/effective-branching-model");
    }

    // The spec is source..destination, and the dots have to survive escaping.
    [Fact]
    public async Task File_conflicts_escapes_the_spec_into_one_segment()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await new FileConflictsHandler(Client(http), Creds()).HandleAsync(
            new FileConflictsRequest("ws", "repo", "feature/x..main", 100),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/repo/file-conflicts/feature%2Fx..main");
    }

    [Fact]
    public async Task Default_reviewer_view_gets_the_single_reviewer_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"account_id":"557058:abc","display_name":"Jane"}""");

        var result = (dynamic)await new ViewDefaultReviewerHandler(Client(http), Creds()).HandleAsync(
            new ViewDefaultReviewerRequest("ws", "repo", "557058:abc"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri.Should()
            .Be("https://api.bitbucket.org/2.0/repositories/ws/repo/default-reviewers/557058%3Aabc");
        ((string)result.display_name).Should().Be("Jane");
    }

    // Unlike the merging PUTs elsewhere, this one replaces, so the key body has
    // to go out even when only the label is changing.
    [Fact]
    public async Task Deploy_key_update_always_sends_the_key()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":7,"label":"ci","key":"ssh-rsa AAAA"}""");

        await new UpdateRepoDeployKeyHandler(Client(http), Creds()).HandleAsync(
            new UpdateRepoDeployKeyRequest("ws", "repo", 7, "ssh-rsa AAAA", "ci"),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Put);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/deploy-keys/7");
        var body = Body(http);
        body.GetProperty("key").GetString().Should().Be("ssh-rsa AAAA");
        body.GetProperty("label").GetString().Should().Be("ci");
    }

    [Fact]
    public async Task A_missing_repository_fails_before_any_call_goes_out()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new ViewOverrideSettingsHandler(Client(http), NoWorkspace())
            .HandleAsync(new ViewOverrideSettingsRequest(null, null), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }
}
