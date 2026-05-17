using System.Net;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.Hooks.CreateRepoHook;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Repos.Hooks;

public class CreateRepoHookHandlerTests
{
    [Fact]
    public async Task HandleAsync_posts_payload_with_default_events_when_none_supplied()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.Created,
            """{"uuid":"{u1}","description":"d","url":"https://example/h","active":true,"events":["repo:push"]}""");

        await handler.HandleAsync(
            new CreateRepoHookRequest("ws", "myrepo", "https://example/h", "d", null, true),
            CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/hooks");
        var bodyJson = JsonDocument.Parse(http.CallBodies.Single()!);
        bodyJson.RootElement.GetProperty("url").GetString().Should().Be("https://example/h");
        bodyJson.RootElement.GetProperty("description").GetString().Should().Be("d");
        bodyJson.RootElement.GetProperty("active").GetBoolean().Should().BeTrue();
        bodyJson.RootElement.GetProperty("events").EnumerateArray().Select(e => e.GetString())
            .Should().Equal("repo:push");
    }

    [Fact]
    public async Task HandleAsync_posts_explicit_events_when_supplied()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.Created,
            """{"uuid":"{u1}","url":"https://e","active":true,"events":["pullrequest:created"]}""");

        await handler.HandleAsync(
            new CreateRepoHookRequest("ws", "myrepo", "https://e", null,
                new[] { "pullrequest:created", "pullrequest:approved" }, false),
            CancellationToken.None);

        var bodyJson = JsonDocument.Parse(http.CallBodies.Single()!);
        bodyJson.RootElement.GetProperty("events").EnumerateArray().Select(e => e.GetString())
            .Should().Equal("pullrequest:created", "pullrequest:approved");
        bodyJson.RootElement.GetProperty("active").GetBoolean().Should().BeFalse();
        bodyJson.RootElement.TryGetProperty("description", out _).Should().BeFalse();
    }

    private static CreateRepoHookHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new CreateRepoHookHandler(client, credentials);
    }
}
