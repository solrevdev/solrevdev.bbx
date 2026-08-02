using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Workspaces.Hooks.ListWorkspaceHooks;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Workspaces.Hooks;

public class ListWorkspaceHooksHandlerTests
{
    [Fact]
    public async Task HandleAsync_uses_default_workspace_when_not_specified()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "default-ws" });
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await handler.HandleAsync(new ListWorkspaceHooksRequest(null, 25), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/default-ws/hooks");
    }

    [Fact]
    public async Task HandleAsync_throws_when_workspace_missing()
    {
        var handler = BuildHandler(out _, seed: null);

        var act = async () => await handler.HandleAsync(new ListWorkspaceHooksRequest(null, 25), TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("Error: Workspace required.*");
    }

    [Fact]
    public async Task HandleAsync_stops_at_limit()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"uuid":"{u1}","url":"https://example.com/1","active":true,"events":["repo:push"]},
                {"uuid":"{u2}","url":"https://example.com/2","active":false,"events":["pullrequest:created"]},
                {"uuid":"{u3}","url":"https://example.com/3","active":true,"events":["pullrequest:approved"]}
            ],"next":null}
            """);

        var result = (dynamic)await handler.HandleAsync(new ListWorkspaceHooksRequest("ws", 2), TestContext.Current.CancellationToken);

        ((string)result.workspace).Should().Be("ws");
        ((int)result.count).Should().Be(2);
    }

    private static ListWorkspaceHooksHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new ListWorkspaceHooksHandler(client, credentials);
    }
}
