using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Commits.Comments.AddCommitComment;
using Bbx.Features.Commits.Comments.DeleteCommitComment;
using Bbx.Features.Commits.Comments.UpdateCommitComment;
using Bbx.Features.Commits.Comments.ViewCommitComment;
using Bbx.Features.Commits.ListCommits;
using Bbx.Features.Commits.ViewCommitStatus;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Commits;

public class CommitCommentHandlerTests
{
    private const string Hash = "abc123";

    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    private static JsonElement Body(FakeHttpMessageHandler http)
        => JsonDocument.Parse(http.CallBodies[0]!).RootElement;

    [Fact]
    public async Task Comment_posts_the_raw_content()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Created, """{"id":1}""");

        await new AddCommitCommentHandler(Client(http), Creds()).HandleAsync(
            new AddCommitCommentRequest("ws", "repo", Hash, "looks good", null, null),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Post);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/commit/abc123/comments");
        var body = Body(http);
        body.GetProperty("content").GetProperty("raw").GetString().Should().Be("looks good");
        body.TryGetProperty("inline", out _).Should().BeFalse();
    }

    // Bitbucket anchors a comment through an "inline" object, and calls the
    // post-commit line "to". Sending "line" is silently ignored.
    [Fact]
    public async Task Comment_anchors_to_a_file_and_line_through_inline()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Created, """{"id":1}""");

        await new AddCommitCommentHandler(Client(http), Creds()).HandleAsync(
            new AddCommitCommentRequest("ws", "repo", Hash, "here", "src/a.cs", 42),
            TestContext.Current.CancellationToken);

        var inline = Body(http).GetProperty("inline");
        inline.GetProperty("path").GetString().Should().Be("src/a.cs");
        inline.GetProperty("to").GetInt32().Should().Be(42);
    }

    [Fact]
    public async Task Comment_anchors_to_a_whole_file_when_no_line_is_given()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Created, """{"id":1}""");

        await new AddCommitCommentHandler(Client(http), Creds()).HandleAsync(
            new AddCommitCommentRequest("ws", "repo", Hash, "here", "src/a.cs", null),
            TestContext.Current.CancellationToken);

        var inline = Body(http).GetProperty("inline");
        inline.GetProperty("path").GetString().Should().Be("src/a.cs");
        inline.TryGetProperty("to", out _).Should().BeFalse();
    }

    // A line with no file anchors to nothing. Bitbucket accepts the body and
    // drops the anchor, so the comment lands unanchored and looks fine.
    [Fact]
    public async Task A_line_without_a_path_fails_before_calling_the_api()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new AddCommitCommentHandler(Client(http), Creds()).HandleAsync(
            new AddCommitCommentRequest("ws", "repo", Hash, "here", null, 42),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(AddCommitCommentHandler.LineNeedsPath);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Comment_view_gets_the_single_comment_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":5}""");

        await new ViewCommitCommentHandler(Client(http), Creds()).HandleAsync(
            new ViewCommitCommentRequest("ws", "repo", Hash, 5), TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Get);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/commit/abc123/comments/5");
    }

    [Fact]
    public async Task Comment_update_puts_only_the_content()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":5}""");

        await new UpdateCommitCommentHandler(Client(http), Creds()).HandleAsync(
            new UpdateCommitCommentRequest("ws", "repo", Hash, 5, "edited"),
            TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Put);
        var body = Body(http);
        body.GetProperty("content").GetProperty("raw").GetString().Should().Be("edited");
        body.EnumerateObject().Should().ContainSingle();
    }

    [Fact]
    public async Task Comment_delete_deletes_the_comment()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");

        await new DeleteCommitCommentHandler(Client(http), Creds()).HandleAsync(
            new DeleteCommitCommentRequest("ws", "repo", Hash, 5), TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Delete);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/commit/abc123/comments/5");
    }

    // A build status key can contain characters that would otherwise split the
    // path, so it is escaped into one segment on the way out.
    [Fact]
    public async Task Status_view_escapes_the_key()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"key":"build/1","state":"SUCCESSFUL"}""");

        await new ViewCommitStatusHandler(Client(http), Creds()).HandleAsync(
            new ViewCommitStatusRequest("ws", "repo", Hash, "build/1"),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri.Should()
            .Be("https://api.bitbucket.org/2.0/repositories/ws/repo/commit/abc123/statuses/build/build%2F1");
    }

    [Fact]
    public async Task Commit_list_stays_a_get_without_include_or_exclude()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await new ListCommitsHandler(Client(http), Creds()).HandleAsync(
            new ListCommitsRequest("ws", "repo", null, null, 25), TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Get);
    }

    // Bitbucket takes include and exclude in a POST body only, so passing
    // either has to switch the verb or they are quietly dropped.
    [Fact]
    public async Task Include_and_exclude_switch_the_call_to_a_post()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[{"hash":"aaaaaaaaaaaaaaa","message":"m"}],"next":null}""");

        await new ListCommitsHandler(Client(http), Creds()).HandleAsync(
            new ListCommitsRequest("ws", "repo", null, null, 25, ["main"], ["release"]),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Post);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/commits");
        var body = Body(http);
        body.GetProperty("include").EnumerateArray().Single().GetString().Should().Be("main");
        body.GetProperty("exclude").EnumerateArray().Single().GetString().Should().Be("release");
    }

    [Fact]
    public async Task A_branch_and_include_together_post_to_the_revision_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await new ListCommitsHandler(Client(http), Creds()).HandleAsync(
            new ListCommitsRequest("ws", "repo", "develop", null, 25, ["main"], null),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Post);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/commits/develop");
    }

    // An absent array option parses to an empty array, so keying off null would
    // turn every plain `commit list` into a POST with an empty walk.
    [Fact]
    public async Task Empty_include_and_exclude_arrays_leave_the_call_a_get()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await new ListCommitsHandler(Client(http), Creds()).HandleAsync(
            new ListCommitsRequest("ws", "repo", null, null, 25, [], []),
            TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Get);
    }

    [Fact]
    public async Task A_walk_stops_at_the_limit_without_asking_for_another_page()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[{"hash":"aaaaaaaaaaaaaaa"},{"hash":"bbbbbbbbbbbbbbb"}],
             "next":"https://api.bitbucket.org/2.0/repositories/ws/repo/commits?page=2"}
            """);

        var result = (dynamic)await new ListCommitsHandler(Client(http), Creds()).HandleAsync(
            new ListCommitsRequest("ws", "repo", null, null, 1, ["main"], null),
            TestContext.Current.CancellationToken);

        ((int)result.count).Should().Be(1);
        http.Calls.Should().ContainSingle();
    }

    // The next link carries the walk in its query string, so the second page is
    // a plain GET even though the first was a POST.
    [Fact]
    public async Task A_walk_follows_its_next_link_with_a_get()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[{"hash":"aaaaaaaaaaaaaaa"}],
             "next":"https://api.bitbucket.org/2.0/repositories/ws/repo/commits?page=2"}
            """);
        http.Enqueue(HttpStatusCode.OK, """{"values":[{"hash":"bbbbbbbbbbbbbbb"}],"next":null}""");

        var result = (dynamic)await new ListCommitsHandler(Client(http), Creds()).HandleAsync(
            new ListCommitsRequest("ws", "repo", null, null, 25, ["main"], null),
            TestContext.Current.CancellationToken);

        ((int)result.count).Should().Be(2);
        http.Calls[0].Method.Should().Be(HttpMethod.Post);
        http.Calls[1].Method.Should().Be(HttpMethod.Get);
    }

    [Fact]
    public async Task A_missing_repository_fails_before_any_call_goes_out()
    {
        var http = new FakeHttpMessageHandler();
        var creds = new CredentialManager(new InMemoryCredentialStore());

        var act = async () => await new AddCommitCommentHandler(Client(http), creds).HandleAsync(
            new AddCommitCommentRequest(null, null, Hash, "x", null, null),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }
}
