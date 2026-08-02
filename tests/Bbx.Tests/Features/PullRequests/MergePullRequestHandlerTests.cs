using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.PullRequests.MergePullRequest;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.PullRequests;

public class MergePullRequestHandlerTests
{
    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    // Regression: the command defaulted to "merge", which Bitbucket rejects with
    // "merge_strategy: Select a valid choice. merge is not one of the available
    // choices." The UI's "merge commit" is merge_commit on the wire.
    [Theory]
    [InlineData("merge", "merge_commit")]
    [InlineData("merge_commit", "merge_commit")]
    [InlineData("", "merge_commit")]
    [InlineData(null, "merge_commit")]
    [InlineData("squash", "squash")]
    [InlineData("fast_forward", "fast_forward")]
    [InlineData("fast-forward", "fast_forward")]
    [InlineData("ff", "fast_forward")]
    [InlineData("MERGE", "merge_commit")]
    public void NormalizeStrategy_maps_to_values_bitbucket_accepts(string? input, string expected)
    {
        MergePullRequestHandler.NormalizeStrategy(input).Should().Be(expected);
    }

    [Fact]
    public async Task Merge_sends_the_normalized_strategy()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":1,"state":"MERGED"}""");
        using var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        var handler = new MergePullRequestHandler(client, Creds());

        await handler.HandleAsync(
            new MergePullRequestRequest("ws", "repo", 1, "merge", null, false), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/pullrequests/1/merge");
        http.CallBodies.Single()!.Should().Contain("\"merge_strategy\": \"merge_commit\"");
    }
}
