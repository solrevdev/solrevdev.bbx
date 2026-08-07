using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.PullRequests.MergePullRequest;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.PullRequests;

public class MergePullRequestHandlerTests
{
    private const string PullRequest = """{"id":1,"destination":{"branch":{"name":"master"}}}""";

    private static string Branch(string defaultStrategy) =>
        $$"""{"name":"master","default_merge_strategy":"{{defaultStrategy}}"}""";

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    // Regression: the command defaulted to "merge", which Bitbucket rejects with
    // "merge_strategy: Select a valid choice. merge is not one of the available
    // choices." The UI's "merge commit" is merge_commit on the wire.
    [Theory]
    [InlineData("merge", "merge_commit")]
    [InlineData("merge_commit", "merge_commit")]
    [InlineData("squash", "squash")]
    [InlineData("fast_forward", "fast_forward")]
    [InlineData("fast-forward", "fast_forward")]
    [InlineData("ff", "fast_forward")]
    [InlineData("MERGE", "merge_commit")]
    // Bitbucket allows six, not three: refs/branches/master listed
    // squash_fast_forward, rebase_fast_forward and rebase_merge as well.
    [InlineData("squash_fast_forward", "squash_fast_forward")]
    [InlineData("rebase-fast-forward", "rebase_fast_forward")]
    [InlineData("rebase_merge", "rebase_merge")]
    public void NormalizeStrategy_maps_to_values_bitbucket_accepts(string? input, string expected)
    {
        MergePullRequestHandler.NormalizeStrategy(input).Should().Be(expected);
    }

    // An absent strategy has to stay absent this far in, so the handler can tell
    // "not asked" from "asked for merge_commit" and fall back to the branch.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeStrategy_leaves_an_absent_strategy_null(string? input)
    {
        MergePullRequestHandler.NormalizeStrategy(input).Should().BeNull();
    }

    // An explicit strategy is the caller's business, so it costs no lookups.
    [Fact]
    public async Task Merge_sends_the_normalized_strategy_and_looks_nothing_up()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":1,"state":"MERGED"}""");
        var handler = new MergePullRequestHandler(Client(http), Creds());

        await handler.HandleAsync(
            new MergePullRequestRequest("ws", "repo", 1, "merge", null, false),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pullrequests/1/merge");
        http.CallBodies.Single()!.Should().Contain("\"merge_strategy\": \"merge_commit\"");
    }

    // Regression: --strategy defaulted to merge_commit whatever the repository
    // said, so a repository whose destination branch defaults to squash was
    // merged the wrong way round without a word.
    [Fact]
    public async Task Merge_without_a_strategy_takes_the_destination_branch_default()
    {
        var http = Http(Branch("squash"));
        var handler = new MergePullRequestHandler(Client(http), Creds());

        await handler.HandleAsync(
            new MergePullRequestRequest("ws", "repo", 1, null, null, false),
            TestContext.Current.CancellationToken);

        http.Calls[0].RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/pullrequests/1");
        http.Calls[1].RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/refs/branches/master");
        http.CallBodies[2]!.Should().Contain("\"merge_strategy\": \"squash\"");
    }

    // A branch that reports no default is not a reason to refuse: fall back to
    // the one Bitbucket itself defaults to.
    [Fact]
    public async Task Merge_falls_back_when_the_branch_reports_no_default()
    {
        var http = Http("""{"name":"master"}""");
        var handler = new MergePullRequestHandler(Client(http), Creds());

        await handler.HandleAsync(
            new MergePullRequestRequest("ws", "repo", 1, null, null, false),
            TestContext.Current.CancellationToken);

        http.CallBodies[2]!.Should().Contain("\"merge_strategy\": \"merge_commit\"");
    }

    // The two lookups are advisory. They did not exist before, so a merge that
    // used to work must not start failing because one of them did.
    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Merge_still_goes_ahead_when_the_pull_request_cannot_be_read(HttpStatusCode failure)
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(failure, """{"error":{"message":"nope"}}""");
        http.Enqueue(HttpStatusCode.OK, """{"id":1,"state":"MERGED"}""");
        var handler = new MergePullRequestHandler(Client(http), Creds());

        await handler.HandleAsync(
            new MergePullRequestRequest("ws", "repo", 1, null, null, false),
            TestContext.Current.CancellationToken);

        http.Calls[1].RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pullrequests/1/merge");
        http.CallBodies[1]!.Should().Contain("\"merge_strategy\": \"merge_commit\"");
    }

    [Fact]
    public async Task Merge_still_goes_ahead_when_the_branch_cannot_be_read()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, PullRequest);
        http.Enqueue(HttpStatusCode.NotFound, """{"error":{"message":"gone"}}""");
        http.Enqueue(HttpStatusCode.OK, """{"id":1,"state":"MERGED"}""");
        var handler = new MergePullRequestHandler(Client(http), Creds());

        await handler.HandleAsync(
            new MergePullRequestRequest("ws", "repo", 1, null, null, false),
            TestContext.Current.CancellationToken);

        http.CallBodies[2]!.Should().Contain("\"merge_strategy\": \"merge_commit\"");
    }

    private static FakeHttpMessageHandler Http(string branch)
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, PullRequest);
        http.Enqueue(HttpStatusCode.OK, branch);
        http.Enqueue(HttpStatusCode.OK, """{"id":1,"state":"MERGED"}""");
        return http;
    }

    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());
}
