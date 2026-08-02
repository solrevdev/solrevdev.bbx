using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.Hooks.ListRepoHooks;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Repos.Hooks;

public class ListRepoHooksHandlerTests
{
    [Fact]
    public async Task HandleAsync_uses_default_workspace_and_repo_path()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "default-ws" });
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await handler.HandleAsync(new ListRepoHooksRequest(null, "myrepo", 25), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/default-ws/myrepo/hooks");
    }

    [Fact]
    public async Task HandleAsync_throws_when_repo_missing()
    {
        var handler = BuildHandler(out _,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });

        var act = async () => await handler.HandleAsync(new ListRepoHooksRequest("ws", null, 25), CancellationToken.None);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("Error: Workspace and repository required.*");
    }

    [Fact]
    public async Task HandleAsync_projects_summary_fields_and_stops_at_limit()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"uuid":"{u1}","description":"hook one","url":"https://example.com/1","active":true,"events":["repo:push"],"created_at":"2026-01-01"},
                {"uuid":"{u2}","description":"hook two","url":"https://example.com/2","active":false,"events":["pullrequest:created"],"created_at":"2026-02-01"},
                {"uuid":"{u3}","description":"hook three","url":"https://example.com/3","active":true,"events":["pullrequest:approved"],"created_at":"2026-03-01"}
            ],"next":null}
            """);

        var result = (dynamic)await handler.HandleAsync(new ListRepoHooksRequest("ws", "myrepo", 2), CancellationToken.None);

        ((string)result.workspace).Should().Be("ws");
        ((string)result.repository).Should().Be("myrepo");
        ((int)result.count).Should().Be(2);
    }

    private static ListRepoHooksHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new ListRepoHooksHandler(client, credentials);
    }
}
