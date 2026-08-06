using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Source.LsSource;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Source;

public class LsSourceHandlerTests
{
    [Fact]
    public async Task HandleAsync_composes_url_with_ref_and_path_and_no_trailing_slash_at_root()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await handler.HandleAsync(new LsSourceRequest("ws", "myrepo", "main", null, 100), TestContext.Current.CancellationToken);

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
            TestContext.Current.CancellationToken);

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
            new LsSourceRequest("ws", "myrepo", "main", null, 2), TestContext.Current.CancellationToken);

        ((int)result.count).Should().Be(2);
        ((string)result.@ref).Should().Be("main");
    }

    // Without a ref, Bitbucket serves the root of the main branch from the bare
    // /src endpoint, which saves the caller looking up whether the branch is
    // called main or master.
    [Fact]
    public async Task HandleAsync_lists_the_main_branch_root_when_no_ref_is_given()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await handler.HandleAsync(new LsSourceRequest("ws", "myrepo", null, null, 100),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/src");
    }

    // A path with no ref has nothing to hang off, and the bare endpoint ignores
    // one rather than failing, so it is refused here instead.
    [Fact]
    public async Task HandleAsync_refuses_a_path_without_a_ref()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });

        var act = async () => await handler.HandleAsync(
            new LsSourceRequest("ws", "myrepo", null, "src", 100), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_throws_when_workspace_or_repo_missing()
    {
        var handler = BuildHandler(out _, seed: null);

        var act = async () => await handler.HandleAsync(
            new LsSourceRequest(null, null, "main", null, 100), TestContext.Current.CancellationToken);

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
