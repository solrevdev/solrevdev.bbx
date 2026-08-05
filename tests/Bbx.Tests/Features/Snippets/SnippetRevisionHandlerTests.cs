using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Snippets.DeleteSnippet;
using Bbx.Features.Snippets.SnippetCommits;
using Bbx.Features.Snippets.SnippetDiff;
using Bbx.Features.Snippets.ViewSnippet;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Snippets;

/// <summary>
/// Snippets pinned to a revision, and the commit log, diff and patch that
/// were missing entirely.
/// </summary>
public class SnippetRevisionHandlerTests
{
    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    [Fact]
    public async Task View_without_a_revision_reads_the_latest()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":"abc","title":"t"}""");

        await new ViewSnippetHandler(Client(http), Creds()).HandleAsync(
            new ViewSnippetRequest("abc", "ws"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath.Should().Be("/2.0/snippets/ws/abc");
    }

    // Bitbucket takes the revision as an extra path segment, not a query
    // parameter.
    [Fact]
    public async Task View_with_a_revision_pins_to_that_node()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":"abc","title":"t"}""");

        await new ViewSnippetHandler(Client(http), Creds()).HandleAsync(
            new ViewSnippetRequest("abc", "ws", "d3adb33f"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath.Should().Be("/2.0/snippets/ws/abc/d3adb33f");
    }

    [Fact]
    public async Task Delete_with_a_revision_pins_to_that_node()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");

        await new DeleteSnippetHandler(Client(http), Creds()).HandleAsync(
            new DeleteSnippetRequest("abc", "ws", "d3adb33f"), TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Delete);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/snippets/ws/abc/d3adb33f");
    }

    [Fact]
    public async Task Commits_lists_the_log()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[{"hash":"a"},{"hash":"b"}],"next":null}""");

        var result = (dynamic)await new SnippetCommitsHandler(Client(http), Creds()).HandleAsync(
            new SnippetCommitsRequest("abc", "ws", null, 25), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath.Should().Be("/2.0/snippets/ws/abc/commits");
        ((int)result.count).Should().Be(2);
    }

    [Fact]
    public async Task Commits_with_a_revision_reads_one_commit()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"hash":"a"}""");

        await new SnippetCommitsHandler(Client(http), Creds()).HandleAsync(
            new SnippetCommitsRequest("abc", "ws", "a", 25), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath.Should().Be("/2.0/snippets/ws/abc/commits/a");
    }

    [Theory]
    [InlineData(false, "/2.0/snippets/ws/abc/rev1/diff")]
    [InlineData(true, "/2.0/snippets/ws/abc/rev1/patch")]
    public async Task Diff_and_patch_differ_only_in_the_trailing_segment(bool asPatch, string expected)
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, "diff --git a b", "text/plain");

        await new SnippetDiffHandler(Client(http), Creds()).HandleAsync(
            new SnippetDiffRequest("abc", "ws", "rev1", asPatch), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath.Should().Be(expected);
    }

    [Fact]
    public async Task A_missing_workspace_fails_before_any_call_goes_out()
    {
        var http = new FakeHttpMessageHandler();
        var creds = new CredentialManager(new InMemoryCredentialStore());

        var act = async () => await new SnippetCommitsHandler(Client(http), creds).HandleAsync(
            new SnippetCommitsRequest("abc", null, null, 25), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }
}
