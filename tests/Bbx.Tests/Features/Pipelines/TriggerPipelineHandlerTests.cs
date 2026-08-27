using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Pipelines.TriggerPipeline;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Pipelines;

public sealed class TriggerPipelineHandlerTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var file in _tempFiles)
            File.Delete(file);
    }

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

    // On-demand runs put the YAML in the body, so the target moves into query
    // parameters keyed by the JSON path of the field it replaces.
    [Fact]
    public async Task HandleAsync_on_demand_sends_yaml_body_and_target_as_query_parameters()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"{p1}","build_number":3,"state":{"name":"PENDING"}}""");
        var yamlPath = WriteYaml("pipelines:\n  default:\n    - step:\n        script:\n          - echo on-demand\n");

        await handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", "main", null, null, null, [], yamlPath),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/myrepo/pipelines/");
        call.Content!.Headers.ContentType!.MediaType.Should().Be("application/yaml");
        http.CallBodies.Single().Should().Contain("echo on-demand");

        var query = ParseQuery(call.RequestUri!);
        query["target.type"].Should().Be("pipeline_ref_target");
        query["target.ref_type"].Should().Be("branch");
        query["target.ref_name"].Should().Be("main");
    }

    [Fact]
    public async Task HandleAsync_on_demand_carries_variables_and_optional_query_parameters()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"{p1}"}""");
        var yamlPath = WriteYaml("pipelines:\n  custom:\n    scan:\n      - step:\n          script: [echo hi]\n");

        await handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", "main", null, "Deploy to production", null,
                ["KEY=value", "MY_SECRET=hush"], yamlPath, MergeDefaults: true, TargetBranchToCreate: "od-run"),
            TestContext.Current.CancellationToken);

        var query = ParseQuery(http.Calls.Single().RequestUri!);
        query["target.selector.type"].Should().Be("custom");
        query["target.selector.pattern"].Should().Be("Deploy to production");
        query["variables[0].key"].Should().Be("KEY");
        query["variables[0].value"].Should().Be("value");
        query["variables[0].secured"].Should().Be("false");
        query["variables[1].key"].Should().Be("MY_SECRET");
        query["variables[1].secured"].Should().Be("true");
        query["merge_defaults"].Should().Be("true");
        query["target_branch_to_create"].Should().Be("od-run");
    }

    // The pull-request target keeps its full shape in query form too: both
    // branches, both commits, and the id.
    [Fact]
    public async Task HandleAsync_on_demand_pull_request_target_flattens_to_query_parameters()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.OK, PullRequest);
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"{p1}"}""");
        var yamlPath = WriteYaml("pipelines:\n  pull-requests:\n    '**':\n      - step:\n          script: [echo pr]\n");

        await handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", null, null, null, "123", [], yamlPath),
            TestContext.Current.CancellationToken);

        var query = ParseQuery(http.Calls[1].RequestUri!);
        query["target.type"].Should().Be("pipeline_pullrequest_target");
        query["target.source"].Should().Be("feature/x");
        query["target.destination"].Should().Be("master");
        query["target.commit.hash"].Should().Be("aaa111");
        query["target.destination_commit.hash"].Should().Be("bbb222");
        query["target.pullrequest.id"].Should().Be("123");
    }

    [Fact]
    public async Task HandleAsync_refuses_on_demand_flags_without_yaml()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });

        var act = () => handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", "main", null, null, null, [], null, MergeDefaults: true),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage(TriggerPipelineHandler.OnDemandFlagsNeedYaml);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_reports_a_missing_yaml_file_before_any_network_call()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });

        // No --branch, so the normal path would look the repository up first: a
        // typo'd path must fail on its own error, not after a round trip.
        var act = () => handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", null, null, null, null, [],
                Path.Combine(Path.GetTempPath(), "bbx-no-such-file.yml")),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*YAML file not found*");
        http.Calls.Should().BeEmpty();
    }

    // An empty --yaml is a broken invocation (an unset shell variable, say).
    // Routing it to the config-file path would trigger a real run the caller
    // never asked for.
    [Fact]
    public async Task HandleAsync_refuses_an_empty_yaml_path()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });

        var act = () => handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", "main", null, null, null, [], ""),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage(TriggerPipelineHandler.EmptyYamlPath);
        http.Calls.Should().BeEmpty();
    }

    private string WriteYaml(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"bbx-test-{Guid.NewGuid():N}.yml");
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    private static Dictionary<string, string> ParseQuery(Uri uri) => uri.Query
        .TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(pair => pair.Split('=', 2))
        .ToDictionary(
            parts => Uri.UnescapeDataString(parts[0]),
            parts => parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : string.Empty);

    private static TriggerPipelineHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new TriggerPipelineHandler(client, credentials);
    }
}
