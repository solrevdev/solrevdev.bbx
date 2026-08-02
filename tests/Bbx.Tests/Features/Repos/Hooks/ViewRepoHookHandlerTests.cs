using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.Hooks.ViewRepoHook;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Repos.Hooks;

public class ViewRepoHookHandlerTests
{
    [Fact]
    public async Task HandleAsync_escapes_uid_in_path()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK,
            """{"uuid":"{abc}","description":"x","url":"https://e","active":true,"events":["repo:push"]}""");

        await handler.HandleAsync(new ViewRepoHookRequest("ws", "myrepo", "{abc}"), TestContext.Current.CancellationToken);

        // EscapeDataString encodes both braces.
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/hooks/%7Babc%7D");
    }

    [Fact]
    public async Task HandleAsync_projects_fields()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK,
            """{"uuid":"{abc}","description":"d","url":"https://e/h","active":true,"events":["repo:push","pullrequest:created"],"created_at":"2026-01-01"}""");

        var result = (dynamic)await handler.HandleAsync(new ViewRepoHookRequest("ws", "myrepo", "{abc}"), TestContext.Current.CancellationToken);

        ((string)result.uuid).Should().Be("{abc}");
        ((bool)result.active).Should().BeTrue();
    }

    private static ViewRepoHookHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new ViewRepoHookHandler(client, credentials);
    }
}
