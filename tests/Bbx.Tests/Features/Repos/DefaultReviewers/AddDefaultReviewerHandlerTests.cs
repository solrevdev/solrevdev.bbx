using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.DefaultReviewers.AddDefaultReviewer;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Repos.DefaultReviewers;

public class AddDefaultReviewerHandlerTests
{
    [Fact]
    public async Task HandleAsync_puts_with_target_in_path()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK,
            """{"uuid":"{abc}","account_id":"a1","display_name":"Alice"}""");

        await handler.HandleAsync(
            new AddDefaultReviewerRequest("ws", "myrepo", "{abc}"), TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Put);
        call.RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/default-reviewers/%7Babc%7D");
    }

    [Fact]
    public async Task HandleAsync_throws_when_target_missing()
    {
        var handler = BuildHandler(out _,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });

        var act = async () => await handler.HandleAsync(
            new AddDefaultReviewerRequest("ws", "myrepo", ""), TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*--target*");
    }

    private static AddDefaultReviewerHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new AddDefaultReviewerHandler(client, credentials);
    }
}
