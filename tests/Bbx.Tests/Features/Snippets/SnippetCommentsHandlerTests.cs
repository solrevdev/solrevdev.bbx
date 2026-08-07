using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Snippets.SnippetComments;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Snippets;

/// <summary>
/// One command carries list, add, update, delete and view, picked apart by
/// which flag was given. The order the handler tests those flags in is what
/// these pin down, along with the deleted flag that makes a tombstone
/// readable.
/// </summary>
public class SnippetCommentsHandlerTests
{
    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    private static SnippetCommentsRequest Request(
        string? add = null, int? delete = null, int? update = null,
        string? content = null, int? view = null) =>
        new("abc", "ws", add, delete, update, content, view);

    [Fact]
    public async Task View_reads_one_comment()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK,
            """{"id":42,"content":{"raw":"hi"},"user":{"display_name":"Jane"},"deleted":false}""");

        var result = (dynamic)await new SnippetCommentsHandler(Client(http), Creds()).HandleAsync(
            Request(view: 42), TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Get);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/snippets/ws/abc/comments/42");
        ((string)result.content).Should().Be("hi");
        ((bool)result.deleted).Should().BeFalse();
    }

    // Deleting clears the text but leaves the row, so the caller needs the
    // flag to tell a tombstone from a comment someone left empty.
    [Fact]
    public async Task View_reports_a_tombstone_as_deleted()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":42,"content":{"raw":""},"deleted":true}""");

        var result = (dynamic)await new SnippetCommentsHandler(Client(http), Creds()).HandleAsync(
            Request(view: 42), TestContext.Current.CancellationToken);

        ((bool)result.deleted).Should().BeTrue();
    }

    [Fact]
    public async Task List_carries_the_deleted_flag_through()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK,
            """{"values":[{"id":1,"content":{"raw":""},"deleted":true},{"id":2,"content":{"raw":"x"}}],"next":null}""");

        var result = (dynamic)await new SnippetCommentsHandler(Client(http), Creds()).HandleAsync(
            Request(), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath.Should().Be("/2.0/snippets/ws/abc/comments");
        ((int)result.count).Should().Be(2);
        ((bool)((dynamic)result.comments[0]).deleted).Should().BeTrue();
        ((bool)((dynamic)result.comments[1]).deleted).Should().BeFalse();
    }

    // A comment with no deleted property at all is live, not deleted.
    [Fact]
    public async Task A_missing_deleted_property_reads_as_false()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":42,"content":{"raw":"hi"}}""");

        var result = (dynamic)await new SnippetCommentsHandler(Client(http), Creds()).HandleAsync(
            Request(view: 42), TestContext.Current.CancellationToken);

        ((bool)result.deleted).Should().BeFalse();
    }

    [Fact]
    public async Task Add_wins_over_view_when_both_are_given()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Created, """{"id":7,"content":{"raw":"new"}}""");

        await new SnippetCommentsHandler(Client(http), Creds()).HandleAsync(
            Request(add: "new", view: 42), TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task Delete_wins_over_view_when_both_are_given()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");

        await new SnippetCommentsHandler(Client(http), Creds()).HandleAsync(
            Request(delete: 42, view: 42), TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Delete);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/snippets/ws/abc/comments/42");
    }

    [Fact]
    public async Task A_missing_workspace_fails_before_any_call_goes_out()
    {
        var http = new FakeHttpMessageHandler();
        var creds = new CredentialManager(new InMemoryCredentialStore());

        var act = async () => await new SnippetCommentsHandler(Client(http), creds).HandleAsync(
            new SnippetCommentsRequest("abc", null, null, null, null, null, 42),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }
}
