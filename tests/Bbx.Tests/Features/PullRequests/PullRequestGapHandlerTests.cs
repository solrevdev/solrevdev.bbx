using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.PullRequests.Comments.DeletePullRequestComment;
using Bbx.Features.PullRequests.Comments.ResolvePullRequestComment;
using Bbx.Features.PullRequests.Comments.UpdatePullRequestComment;
using Bbx.Features.PullRequests.Comments.ViewPullRequestComment;
using Bbx.Features.PullRequests.PullRequestActivity;
using Bbx.Features.PullRequests.PullRequestConflicts;
using Bbx.Features.PullRequests.PullRequestDiffstat;
using Bbx.Features.PullRequests.PullRequestMergeStatus;
using Bbx.Features.PullRequests.Tasks.ViewPullRequestTask;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.PullRequests;

/// <summary>
/// The pull request verbs that were missing: bbx could add and list comments
/// but not change one, and could not read a diffstat, a conflict list or a
/// merge task's status at all.
/// </summary>
public class PullRequestGapHandlerTests
{
    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    private static JsonElement Body(FakeHttpMessageHandler http)
        => JsonDocument.Parse(http.CallBodies[0]!).RootElement;

    [Fact]
    public async Task Comment_view_gets_the_single_comment_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":9,"content":{"raw":"hi"}}""");

        await new ViewPullRequestCommentHandler(Client(http), Creds()).HandleAsync(
            new ViewPullRequestCommentRequest("ws", "repo", 1, 9), TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Get);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/pullrequests/1/comments/9");
    }

    // Anchoring is fixed when a comment is created, so an edit carries the text
    // and nothing else. Sending path or line back would be rejected.
    [Fact]
    public async Task Comment_update_puts_only_the_content()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":9}""");

        await new UpdatePullRequestCommentHandler(Client(http), Creds()).HandleAsync(
            new UpdatePullRequestCommentRequest("ws", "repo", 1, 9, "edited"),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Put);
        var body = Body(http);
        body.GetProperty("content").GetProperty("raw").GetString().Should().Be("edited");
        body.EnumerateObject().Should().ContainSingle();
    }

    [Fact]
    public async Task Comment_delete_deletes_the_comment()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");

        var message = await new DeletePullRequestCommentHandler(Client(http), Creds()).HandleAsync(
            new DeletePullRequestCommentRequest("ws", "repo", 1, 9), TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Delete);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/pullrequests/1/comments/9");
        message.Should().Contain("#9");
    }

    // Resolving and reopening are the same resource under POST and DELETE.
    [Theory]
    [InlineData(true, "POST")]
    [InlineData(false, "DELETE")]
    public async Task Resolve_and_unresolve_use_the_same_path_under_different_verbs(bool resolve, string verb)
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":9}""");

        await new ResolvePullRequestCommentHandler(Client(http), Creds()).HandleAsync(
            new ResolvePullRequestCommentRequest("ws", "repo", 1, 9, resolve),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Method.Should().Be(verb);
        call.RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pullrequests/1/comments/9/resolve");
    }

    [Fact]
    public async Task Conflicts_lists_the_conflicting_files()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[{"path":"a.txt"}],"next":null}""");

        var result = (dynamic)await new PullRequestConflictsHandler(Client(http), Creds()).HandleAsync(
            new PullRequestConflictsRequest("ws", "repo", 1, 100), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pullrequests/1/conflicts");
        ((int)result.count).Should().Be(1);
    }

    [Fact]
    public async Task Diffstat_totals_the_line_counts()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"status":"modified","lines_added":4,"lines_removed":2,"new":{"path":"a.txt"},"old":{"path":"a.txt"}},
                {"status":"added","lines_added":10,"lines_removed":0,"new":{"path":"b.txt"},"old":null}
            ],"next":null}
            """);

        var result = (dynamic)await new PullRequestDiffstatHandler(Client(http), Creds()).HandleAsync(
            new PullRequestDiffstatRequest("ws", "repo", 1, 500), TestContext.Current.CancellationToken);

        ((int)result.count).Should().Be(2);
        ((int)result.lines_added).Should().Be(14);
        ((int)result.lines_removed).Should().Be(2);
    }

    // A deleted file carries "new": null. Reading through a JSON null throws, so
    // the path has to fall back to "old" rather than assume the property is an
    // object because it is present.
    [Fact]
    public async Task Diffstat_reads_the_old_path_when_a_file_was_deleted()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[{"status":"removed","lines_added":0,"lines_removed":3,"new":null,"old":{"path":"gone.txt"}}],"next":null}
            """);

        var result = (dynamic)await new PullRequestDiffstatHandler(Client(http), Creds()).HandleAsync(
            new PullRequestDiffstatRequest("ws", "repo", 1, 500), TestContext.Current.CancellationToken);

        ((string)result.files[0].path).Should().Be("gone.txt");
    }

    [Fact]
    public async Task Merge_status_escapes_the_task_id()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"task_status":"PENDING"}""");

        await new PullRequestMergeStatusHandler(Client(http), Creds()).HandleAsync(
            new PullRequestMergeStatusRequest("ws", "repo", 1, "{abc-123}"),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri.Should()
            .Be("https://api.bitbucket.org/2.0/repositories/ws/repo/pullrequests/1/merge/task-status/%7Babc-123%7D");
    }

    [Fact]
    public async Task Task_view_gets_the_single_task_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":4,"state":"UNRESOLVED"}""");

        await new ViewPullRequestTaskHandler(Client(http), Creds()).HandleAsync(
            new ViewPullRequestTaskRequest("ws", "repo", 1, 4), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pullrequests/1/tasks/4");
    }

    // The repository-wide feed is its own endpoint, not the per-pull-request
    // one with a blank id.
    [Fact]
    public async Task Activity_without_an_id_reads_the_repository_wide_feed()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await new PullRequestActivityHandler(Client(http), Creds()).HandleAsync(
            new PullRequestActivityRequest("ws", "repo", null, 50), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pullrequests/activity");
    }

    [Fact]
    public async Task Activity_with_an_id_still_reads_that_pull_request()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await new PullRequestActivityHandler(Client(http), Creds()).HandleAsync(
            new PullRequestActivityRequest("ws", "repo", 7, 50), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pullrequests/7/activity");
    }

    // The repository-wide feed is unbounded, so the limit has to actually stop
    // it rather than decorate the output.
    [Fact]
    public async Task Activity_stops_at_the_limit()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[{"a":1},{"a":2},{"a":3}],"next":null}""");

        var result = (dynamic)await new PullRequestActivityHandler(Client(http), Creds()).HandleAsync(
            new PullRequestActivityRequest("ws", "repo", null, 2), TestContext.Current.CancellationToken);

        ((int)result.count).Should().Be(2);
    }

    [Fact]
    public async Task A_missing_repository_fails_before_any_call_goes_out()
    {
        var http = new FakeHttpMessageHandler();
        var creds = new CredentialManager(new InMemoryCredentialStore());

        var act = async () => await new ViewPullRequestCommentHandler(Client(http), creds).HandleAsync(
            new ViewPullRequestCommentRequest(null, null, 1, 9), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }
}
