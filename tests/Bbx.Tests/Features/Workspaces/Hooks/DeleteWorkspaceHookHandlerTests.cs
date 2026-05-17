using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Workspaces.Hooks.DeleteWorkspaceHook;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Workspaces.Hooks;

public class DeleteWorkspaceHookHandlerTests
{
    [Fact]
    public async Task HandleAsync_sends_delete_with_escaped_uid()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.NoContent, "");

        var message = await handler.HandleAsync(
            new DeleteWorkspaceHookRequest("ws", "{u1}"), CancellationToken.None);

        http.Calls.Single().Method.Should().Be(HttpMethod.Delete);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/ws/hooks/%7Bu1%7D");
        message.Should().Contain("{u1}").And.Contain("ws");
    }

    [Fact]
    public async Task HandleAsync_throws_when_workspace_missing()
    {
        var handler = BuildHandler(out _, seed: null);

        var act = async () => await handler.HandleAsync(
            new DeleteWorkspaceHookRequest(null, "{u1}"), CancellationToken.None);

        await act.Should().ThrowAsync<BbxUserException>();
    }

    private static DeleteWorkspaceHookHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new DeleteWorkspaceHookHandler(client, credentials);
    }
}
