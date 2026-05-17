using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Source.LsSource;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Source;

public class LsSourceHandlerTests
{
    [Fact]
    public async Task HandleAsync_composes_url_with_ref_and_path_and_no_trailing_slash_at_root()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await handler.HandleAsync(new LsSourceRequest("ws", "myrepo", "main", null, 100), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/src/main/");
    }

    [Fact]
    public async Task HandleAsync_escapes_path_segments_individually()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await handler.HandleAsync(
            new LsSourceRequest("ws", "myrepo", "feature/x", "src/file with spaces", 100),
            CancellationToken.None);

        // Branch name and each path segment escaped; slashes inside path are preserved.
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/src/feature%2Fx/src/file%20with%20spaces");
    }

    [Fact]
    public async Task HandleAsync_projects_tree_entries_and_stops_at_limit()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"type":"commit_directory","path":"src","commit":{"hash":"abcdef"}},
                {"type":"commit_file","path":"README.md","size":1024,"commit":{"hash":"abcdef"}},
                {"type":"commit_file","path":"LICENSE","size":2048,"commit":{"hash":"abcdef"}}
            ],"next":null}
            """);

        var result = (dynamic)await handler.HandleAsync(
            new LsSourceRequest("ws", "myrepo", "main", null, 2), CancellationToken.None);

        ((int)result.count).Should().Be(2);
        ((string)result.@ref).Should().Be("main");
    }

    [Fact]
    public async Task HandleAsync_throws_when_workspace_or_repo_missing()
    {
        var handler = BuildHandler(out _, seed: null);

        var act = async () => await handler.HandleAsync(
            new LsSourceRequest(null, null, "main", null, 100), CancellationToken.None);

        await act.Should().ThrowAsync<BbxUserException>();
    }

    private static LsSourceHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new LsSourceHandler(client, credentials);
    }
}
