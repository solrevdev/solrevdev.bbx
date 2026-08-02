using System.Net;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Workspaces.Hooks.UpdateWorkspaceHook;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Workspaces.Hooks;

public class UpdateWorkspaceHookHandlerTests
{
    [Fact]
    public async Task HandleAsync_puts_only_the_supplied_fields()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.OK,
            """{"uuid":"{u1}","description":"d","url":"https://e","active":false,"events":["repo:push"]}""");

        await handler.HandleAsync(
            new UpdateWorkspaceHookRequest("ws", "{u1}", null, null, null, false),
            CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/ws/hooks/%7Bu1%7D");
        http.Calls.Single().Method.Should().Be(HttpMethod.Put);
        var bodyJson = JsonDocument.Parse(http.CallBodies.Single()!);
        bodyJson.RootElement.GetProperty("active").GetBoolean().Should().BeFalse();
        bodyJson.RootElement.TryGetProperty("url", out _).Should().BeFalse();
        bodyJson.RootElement.TryGetProperty("description", out _).Should().BeFalse();
        bodyJson.RootElement.TryGetProperty("events", out _).Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_throws_when_no_field_supplied()
    {
        var handler = BuildHandler(out _,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });

        var act = async () => await handler.HandleAsync(
            new UpdateWorkspaceHookRequest("ws", "{u1}", null, null, null, null),
            CancellationToken.None);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("Error: Provide at least one of *");
    }

    private static UpdateWorkspaceHookHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new UpdateWorkspaceHookHandler(client, credentials);
    }
}
