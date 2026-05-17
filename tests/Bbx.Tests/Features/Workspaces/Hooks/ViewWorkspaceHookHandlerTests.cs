using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Workspaces.Hooks.ViewWorkspaceHook;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Workspaces.Hooks;

public class ViewWorkspaceHookHandlerTests
{
    [Fact]
    public async Task HandleAsync_escapes_uid_and_hits_workspaces_hooks_path()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.OK,
            """{"uuid":"{abc}","description":"d","url":"https://e","active":true,"events":["repo:push"],"created_at":"2026-01-01"}""");

        var result = (dynamic)await handler.HandleAsync(
            new ViewWorkspaceHookRequest("ws", "{abc}"), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/ws/hooks/%7Babc%7D");
        ((string)result.uuid).Should().Be("{abc}");
    }

    private static ViewWorkspaceHookHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new ViewWorkspaceHookHandler(client, credentials);
    }
}
