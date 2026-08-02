using System.Net;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Workspaces.Hooks.CreateWorkspaceHook;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Workspaces.Hooks;

public class CreateWorkspaceHookHandlerTests
{
    [Fact]
    public async Task HandleAsync_posts_default_events_when_none_supplied()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.Created,
            """{"uuid":"{u1}","description":"d","url":"https://e","active":true,"events":["repo:push"]}""");

        await handler.HandleAsync(
            new CreateWorkspaceHookRequest("ws", "https://e", "d", null, true),
            CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/ws/hooks");
        var bodyJson = JsonDocument.Parse(http.CallBodies.Single()!);
        bodyJson.RootElement.GetProperty("url").GetString().Should().Be("https://e");
        bodyJson.RootElement.GetProperty("description").GetString().Should().Be("d");
        bodyJson.RootElement.GetProperty("active").GetBoolean().Should().BeTrue();
        bodyJson.RootElement.GetProperty("events").EnumerateArray().Select(e => e.GetString())
            .Should().Equal("repo:push");
    }

    [Fact]
    public async Task HandleAsync_posts_explicit_events_when_supplied()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.Created,
            """{"uuid":"{u1}","url":"https://e","active":false,"events":["pullrequest:created"]}""");

        await handler.HandleAsync(
            new CreateWorkspaceHookRequest("ws", "https://e", null,
                new[] { "pullrequest:created", "pullrequest:approved" }, false),
            CancellationToken.None);

        var bodyJson = JsonDocument.Parse(http.CallBodies.Single()!);
        bodyJson.RootElement.GetProperty("events").EnumerateArray().Select(e => e.GetString())
            .Should().Equal("pullrequest:created", "pullrequest:approved");
        bodyJson.RootElement.GetProperty("active").GetBoolean().Should().BeFalse();
        bodyJson.RootElement.TryGetProperty("description", out _).Should().BeFalse();
    }

    private static CreateWorkspaceHookHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new CreateWorkspaceHookHandler(client, credentials);
    }
}
