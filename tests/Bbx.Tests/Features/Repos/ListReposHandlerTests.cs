using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.ListRepos;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Repos;

public class ListReposHandlerTests
{
    [Fact]
    public async Task HandleAsync_uses_default_workspace_when_request_workspace_is_null()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "default-ws" });
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await handler.HandleAsync(new ListReposRequest(null, 25, null), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/default-ws");
    }

    [Fact]
    public async Task HandleAsync_throws_when_no_workspace_resolvable()
    {
        var handler = BuildHandler(out _, seed: null);

        var act = async () => await handler.HandleAsync(new ListReposRequest(null, 25, null), CancellationToken.None);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("Error: Workspace required.*");
    }

    [Fact]
    public async Task HandleAsync_escapes_query_string()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await handler.HandleAsync(new ListReposRequest("ws", 5, "name~\"api\""), CancellationToken.None);

        // Uri.EscapeDataString leaves `~` alone (RFC 3986 unreserved) and escapes the quotes.
        http.Calls.Single().RequestUri!.Query
            .Should().Be("?q=name~%22api%22");
    }

    [Fact]
    public async Task HandleAsync_stops_at_limit_and_projects_summary_fields()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"name":"alpha","slug":"alpha","full_name":"ws/alpha","is_private":true,"scm":"git","description":"A","updated_on":"2026-01-01","size":123},
                {"name":"beta","slug":"beta","full_name":"ws/beta","is_private":false,"scm":"git","description":null,"updated_on":"2026-02-01","size":456},
                {"name":"gamma","slug":"gamma","full_name":"ws/gamma","is_private":false,"scm":"git","description":null,"updated_on":"2026-03-01","size":789}
            ],"next":null}
            """);

        var result = (dynamic)await handler.HandleAsync(new ListReposRequest("ws", 2, null), CancellationToken.None);

        ((string)result.workspace).Should().Be("ws");
        ((int)result.count).Should().Be(2);
        var repos = (System.Collections.IEnumerable)result.repositories;
        repos.Cast<object>().Should().HaveCount(2);
    }

    private static ListReposHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new ListReposHandler(client, credentials);
    }
}
