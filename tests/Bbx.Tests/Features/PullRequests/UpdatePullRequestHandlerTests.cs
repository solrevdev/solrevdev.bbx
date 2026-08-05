using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.PullRequests.UpdatePullRequest;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.PullRequests;

public class UpdatePullRequestHandlerTests
{
    private const string Ok = """{"id":1,"state":"OPEN","title":"t"}""";

    /// A pull request as the API reports it, for the read that guards
    /// close_source_branch.
    private const string Current = """
        {"id":1,"state":"OPEN","title":"Existing title","description":"Existing description",
         "destination":{"branch":{"name":"main"}}}
        """;

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    private static UpdatePullRequestHandler Handler(FakeHttpMessageHandler http) =>
        new(new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()), Creds());

    private static UpdatePullRequestRequest Request(
        string? title = null,
        string? body = null,
        string? destination = null,
        string[]? reviewers = null,
        bool? closeSourceBranch = null)
        => new("ws", "repo", 1, title, body, destination, reviewers, closeSourceBranch);

    private static JsonElement Body(FakeHttpMessageHandler http, int index = 0)
        => JsonDocument.Parse(http.CallBodies[index]!).RootElement;

    [Fact]
    public async Task Update_puts_to_the_pull_request_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(Request(title: "New"), TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Put);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/pullrequests/1");
    }

    // The API merges: anything left out of the body keeps its current value.
    // Sending untouched fields would let bbx overwrite a description the caller
    // never mentioned, so the body carries only what was asked for.
    [Fact]
    public async Task Update_sends_only_the_fields_the_caller_set()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(Request(title: "New"), TestContext.Current.CancellationToken);

        var body = Body(http);
        body.GetProperty("title").GetString().Should().Be("New");
        body.TryGetProperty("description", out _).Should().BeFalse();
        body.TryGetProperty("destination", out _).Should().BeFalse();
        body.TryGetProperty("reviewers", out _).Should().BeFalse();
        body.TryGetProperty("close_source_branch", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Update_maps_body_to_description()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(Request(body: "Some text"), TestContext.Current.CancellationToken);

        Body(http).GetProperty("description").GetString().Should().Be("Some text");
    }

    // An empty --body is a real instruction, not an absent one: Bitbucket accepts
    // "" and clears the description. Treating it as unset would make the
    // documented way to clear a description do nothing.
    [Fact]
    public async Task Update_sends_an_empty_description_rather_than_dropping_it()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(Request(body: ""), TestContext.Current.CancellationToken);

        var body = Body(http);
        body.TryGetProperty("description", out var description).Should().BeTrue();
        description.GetString().Should().BeEmpty();
    }

    [Fact]
    public async Task Update_nests_the_destination_branch()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(
            Request(destination: "release/next"), TestContext.Current.CancellationToken);

        Body(http).GetProperty("destination").GetProperty("branch").GetProperty("name")
            .GetString().Should().Be("release/next");
    }

    [Fact]
    public async Task Update_sends_reviewers_as_account_ids()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(
            Request(reviewers: ["557058:abc", "557058:def"]), TestContext.Current.CancellationToken);

        var reviewers = Body(http).GetProperty("reviewers");
        reviewers.EnumerateArray().Select(r => r.GetProperty("account_id").GetString())
            .Should().Equal("557058:abc", "557058:def");
    }

    // Regression: --reviewers parses to an empty array when the caller omits it,
    // so keying off null sent "reviewers": [] with every edit and wiped the
    // reviewers from any pull request that had them.
    [Fact]
    public async Task An_empty_reviewers_array_is_not_sent()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(
            Request(title: "New", reviewers: []), TestContext.Current.CancellationToken);

        Body(http).TryGetProperty("reviewers", out _).Should().BeFalse();
    }

    [Fact]
    public async Task An_empty_reviewers_array_does_not_count_as_something_to_update()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await Handler(http).HandleAsync(
            Request(reviewers: []), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(UpdatePullRequestHandler.NothingToUpdate);
        http.Calls.Should().BeEmpty();
    }

    // Bitbucket applies close_source_branch only when the same PUT moves another
    // field to a new value. On its own it is silently dropped: the 200 echoes
    // the value back and a later GET shows the old setting. Reporting success
    // for that would be a lie, so the handler refuses.
    [Fact]
    public async Task Close_source_branch_on_its_own_is_refused()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Current);

        var act = async () => await Handler(http).HandleAsync(
            Request(closeSourceBranch: false), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(UpdatePullRequestHandler.CloseSourceBranchNeedsCompany);
        http.Calls.Should().ContainSingle().Which.Method.Should().Be(HttpMethod.Get);
    }

    // A companion field set to the value it already holds is not a change, so
    // Bitbucket drops the flag just the same.
    [Theory]
    [InlineData("Existing title", null, null)]
    [InlineData(null, "Existing description", null)]
    [InlineData(null, null, "main")]
    public async Task Close_source_branch_is_refused_when_no_other_field_would_move(
        string? title, string? body, string? destination)
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Current);

        var act = async () => await Handler(http).HandleAsync(
            Request(title: title, body: body, destination: destination, closeSourceBranch: true),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(UpdatePullRequestHandler.CloseSourceBranchNeedsCompany);
    }

    [Theory]
    [InlineData("A different title", null, null)]
    [InlineData(null, "A different description", null)]
    [InlineData(null, null, "release/next")]
    public async Task Close_source_branch_goes_through_when_another_field_moves(
        string? title, string? body, string? destination)
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Current);
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(
            Request(title: title, body: body, destination: destination, closeSourceBranch: true),
            TestContext.Current.CancellationToken);

        http.Calls.Should().HaveCount(2);
        http.Calls[1].Method.Should().Be(HttpMethod.Put);
        Body(http, 1).GetProperty("close_source_branch").GetBoolean().Should().BeTrue();
    }

    // Clearing a description is a change when the pull request has one.
    [Fact]
    public async Task An_emptied_description_counts_as_a_change()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Current);
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(
            Request(body: "", closeSourceBranch: false), TestContext.Current.CancellationToken);

        http.Calls.Should().HaveCount(2);
        http.Calls[1].Method.Should().Be(HttpMethod.Put);
    }

    // A pull request with no description reads back as absent rather than "",
    // so clearing an already-empty description must not count as a change.
    [Fact]
    public async Task An_empty_description_on_a_pull_request_without_one_is_not_a_change()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":1,"state":"OPEN","title":"Existing title"}""");

        var act = async () => await Handler(http).HandleAsync(
            Request(body: "", closeSourceBranch: false), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(UpdatePullRequestHandler.CloseSourceBranchNeedsCompany);
    }

    // Without the flag there is nothing to guard, so no read is needed.
    [Fact]
    public async Task An_update_without_the_flag_does_not_read_first()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(
            Request(title: "New"), TestContext.Current.CancellationToken);

        http.Calls.Should().ContainSingle().Which.Method.Should().Be(HttpMethod.Put);
    }

    // An empty PUT is accepted by Bitbucket and changes nothing, so it would
    // report success for a command that did no work.
    [Fact]
    public async Task Update_with_no_fields_fails_before_calling_the_api()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await Handler(http).HandleAsync(
            Request(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(UpdatePullRequestHandler.NothingToUpdate);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_requires_a_repository()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await Handler(http).HandleAsync(
            new UpdatePullRequestRequest("ws", null, 1, "New", null, null, null, null),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }
}
