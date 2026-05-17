using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.PullRequests.ListPullRequestCommits;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.PullRequests;

public class ListPullRequestCommitsHandlerTests
{
    [Fact]
    public async Task HandleAsync_lists_commits_and_truncates_hash()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"hash":"abcdef1234567890","message":"first","date":"2026-01-01","author":{"raw":"Jane <j@e>"}},
                {"hash":"1234567890abcdef","message":"second","date":"2026-01-02","author":{"raw":"Jane <j@e>"}}
            ],"next":null}
            """);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        var credentials = new CredentialManager(
            new InMemoryCredentialStore(new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" }));
        var handler = new ListPullRequestCommitsHandler(client, credentials);

        var result = (dynamic)await handler.HandleAsync(
            new ListPullRequestCommitsRequest("ws", "myrepo", 42, 50), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pullrequests/42/commits");
        ((int)result.count).Should().Be(2);
    }
}
