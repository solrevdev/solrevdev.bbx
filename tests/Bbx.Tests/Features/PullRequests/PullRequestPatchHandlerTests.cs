using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.PullRequests.PullRequestPatch;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.PullRequests;

public class PullRequestPatchHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_raw_patch_body()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, "From abcdef\nSubject: [PATCH] x\n", "text/plain");
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        var credentials = new CredentialManager(
            new InMemoryCredentialStore(new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" }));
        var handler = new PullRequestPatchHandler(client, credentials);

        var body = await handler.HandleAsync(
            new PullRequestPatchRequest("ws", "myrepo", 42), TestContext.Current.CancellationToken);

        body.Should().StartWith("From abcdef");
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pullrequests/42/patch");
    }
}
