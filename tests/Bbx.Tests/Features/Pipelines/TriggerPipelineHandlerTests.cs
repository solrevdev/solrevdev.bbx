using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Pipelines.TriggerPipeline;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Pipelines;

public class TriggerPipelineHandlerTests
{
    [Fact]
    public async Task HandleAsync_branch_trigger_uses_pipeline_ref_target()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"{p1}","build_number":1,"state":{"name":"PENDING"}}""");

        await handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", "main", null, null, null, []),
            TestContext.Current.CancellationToken);

        var body = JsonDocument.Parse(http.CallBodies.Single()!);
        var target = body.RootElement.GetProperty("target");
        target.GetProperty("type").GetString().Should().Be("pipeline_ref_target");
        target.GetProperty("ref_type").GetString().Should().Be("branch");
        target.GetProperty("ref_name").GetString().Should().Be("main");
        target.TryGetProperty("pullrequest", out _).Should().BeFalse();
        // An explicit branch is taken at its word: no lookup call.
        http.Calls.Should().ContainSingle();
    }

    // Regression: --branch defaulted to "main". A repository whose default
    // branch is "master" answered 404, and one carrying a stale "main" beside a
    // live "master" ran against the wrong branch and reported success.
    [Fact]
    public async Task HandleAsync_without_a_branch_asks_the_repository_for_its_main_branch()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.OK, """{"mainbranch":{"name":"master"}}""");
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"{p1}","build_number":1,"state":{"name":"PENDING"}}""");

        await handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", null, null, null, null, []),
            TestContext.Current.CancellationToken);

        http.Calls[0].RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/myrepo");
        var body = JsonDocument.Parse(http.CallBodies[1]!);
        body.RootElement.GetProperty("target").GetProperty("ref_name").GetString().Should().Be("master");
    }

    // Bitbucket sends an explicit null for a repository with no main branch, so
    // this reads through TryGetObject rather than TryGetProperty.
    [Fact]
    public async Task HandleAsync_without_a_branch_reports_a_repository_that_has_no_main_branch()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.OK, """{"mainbranch":null}""");

        var act = () => handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", null, null, null, null, []),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*ws/myrepo reports no main branch*");
        http.Calls.Should().ContainSingle();
    }

    private const string PullRequest =
        """
        {"id":123,
         "source":{"branch":{"name":"feature/x"},"commit":{"hash":"aaa111"}},
         "destination":{"branch":{"name":"master"},"commit":{"hash":"bbb222"}}}
        """;

    // Regression: this sent a bare "pipeline_pullrequest_target" with a source
    // and an id, which Bitbucket answers with 400 in every spelling. The real
    // target needs both branches and both commits. A ref target with a
    // pull-requests selector runs the same steps and looked right, but the run
    // is not a pull-request run: BITBUCKET_PR_ID came out empty in a live build.
    [Fact]
    public async Task HandleAsync_pull_request_trigger_carries_both_branches_and_both_commits()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.OK, PullRequest);
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"{p1}","build_number":2,"state":{"name":"PENDING"}}""");

        await handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", null, null, null, "123", []),
            TestContext.Current.CancellationToken);

        http.Calls[0].RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/myrepo/pullrequests/123");
        var target = JsonDocument.Parse(http.CallBodies[1]!).RootElement.GetProperty("target");
        target.GetProperty("type").GetString().Should().Be("pipeline_pullrequest_target");
        target.GetProperty("source").GetString().Should().Be("feature/x");
        target.GetProperty("destination").GetString().Should().Be("master");
        target.GetProperty("commit").GetProperty("hash").GetString().Should().Be("aaa111");
        target.GetProperty("destination_commit").GetProperty("hash").GetString().Should().Be("bbb222");
        target.GetProperty("pullrequest").GetProperty("id").GetString().Should().Be("123");
        // selector is the only optional field, so it stays off unless asked for.
        target.TryGetProperty("selector", out _).Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_pull_request_with_pattern_emits_pullrequests_selector()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.OK, PullRequest);
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"{p1}"}""");

        await handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", null, null, "custom-step", "123", []),
            TestContext.Current.CancellationToken);

        var selector = JsonDocument.Parse(http.CallBodies[1]!)
            .RootElement.GetProperty("target").GetProperty("selector");
        selector.GetProperty("type").GetString().Should().Be("pull-requests");
        selector.GetProperty("pattern").GetString().Should().Be("custom-step");
    }

    // The branches come from the pull request, so a --branch beside it is a
    // contradiction rather than an override.
    [Fact]
    public async Task HandleAsync_refuses_a_branch_beside_a_pull_request()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });

        var act = () => handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", "feature/x", null, null, "123", []),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage(TriggerPipelineHandler.BranchAndPullRequest);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_reports_a_pull_request_missing_a_branch_or_a_commit()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.OK, """{"id":123,"source":{"branch":{"name":"feature/x"}}}""");

        var act = () => handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", null, null, null, "123", []),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*does not report both of its branches*");
        http.Calls.Should().ContainSingle();
    }

    // Bitbucket accepts a commit that is not on the branch it was given and runs
    // it labelled with that branch, so a guessed branch here is silently wrong.
    [Fact]
    public async Task HandleAsync_refuses_a_commit_without_a_branch()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });

        var act = () => handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", null, "abc123", null, null, []),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage(TriggerPipelineHandler.CommitNeedsBranch);
        http.Calls.Should().BeEmpty("nothing should be looked up, let alone triggered");
    }

    [Fact]
    public async Task HandleAsync_allows_a_commit_when_the_branch_is_explicit()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"{p1}"}""");

        await handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", "feature-x", "abc123", null, null, []),
            TestContext.Current.CancellationToken);

        var target = JsonDocument.Parse(http.CallBodies.Single()!).RootElement.GetProperty("target");
        target.GetProperty("ref_name").GetString().Should().Be("feature-x");
        target.GetProperty("commit").GetProperty("hash").GetString().Should().Be("abc123");
    }

    private static TriggerPipelineHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new TriggerPipelineHandler(client, credentials);
    }
}
