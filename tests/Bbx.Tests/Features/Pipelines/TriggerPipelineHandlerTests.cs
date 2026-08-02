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
    }

    [Fact]
    public async Task HandleAsync_pull_request_trigger_uses_pullrequest_target()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"{p1}","build_number":2,"state":{"name":"PENDING"}}""");

        await handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", "feature-x", null, null, "123", []),
            TestContext.Current.CancellationToken);

        var body = JsonDocument.Parse(http.CallBodies.Single()!);
        var target = body.RootElement.GetProperty("target");
        target.GetProperty("type").GetString().Should().Be("pipeline_pullrequest_target");
        target.GetProperty("source").GetString().Should().Be("feature-x");
        target.GetProperty("pullrequest").GetProperty("id").GetString().Should().Be("123");
        target.TryGetProperty("ref_type", out _).Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_pull_request_with_pattern_emits_pullrequests_selector()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"{p1}"}""");

        await handler.HandleAsync(
            new TriggerPipelineRequest("ws", "myrepo", "feature-x", null, "custom-step", "123", []),
            TestContext.Current.CancellationToken);

        var body = JsonDocument.Parse(http.CallBodies.Single()!);
        var selector = body.RootElement.GetProperty("target").GetProperty("selector");
        selector.GetProperty("type").GetString().Should().Be("pull-requests");
        selector.GetProperty("pattern").GetString().Should().Be("custom-step");
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
