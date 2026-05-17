using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.DefaultReviewers.RemoveDefaultReviewer;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Repos.DefaultReviewers;

public class RemoveDefaultReviewerHandlerTests
{
    [Fact]
    public async Task HandleAsync_deletes_with_target_in_path()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.NoContent, "");

        var msg = await handler.HandleAsync(
            new RemoveDefaultReviewerRequest("ws", "myrepo", "{abc}"), CancellationToken.None);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Delete);
        call.RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/default-reviewers/%7Babc%7D");
        msg.Should().Contain("{abc}");
    }

    private static RemoveDefaultReviewerHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new RemoveDefaultReviewerHandler(client, credentials);
    }
}
