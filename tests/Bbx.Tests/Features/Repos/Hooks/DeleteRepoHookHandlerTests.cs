using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.Hooks.DeleteRepoHook;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Repos.Hooks;

public class DeleteRepoHookHandlerTests
{
    [Fact]
    public async Task HandleAsync_sends_delete_with_escaped_uid()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.NoContent, "");

        var message = await handler.HandleAsync(
            new DeleteRepoHookRequest("ws", "myrepo", "{u1}"), TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Delete);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/hooks/%7Bu1%7D");
        message.Should().Contain("{u1}").And.Contain("ws/myrepo");
    }

    [Fact]
    public async Task HandleAsync_throws_when_workspace_and_repo_missing()
    {
        var handler = BuildHandler(out _, seed: null);

        var act = async () => await handler.HandleAsync(
            new DeleteRepoHookRequest(null, null, "{u1}"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
    }

    private static DeleteRepoHookHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new DeleteRepoHookHandler(client, credentials);
    }
}
