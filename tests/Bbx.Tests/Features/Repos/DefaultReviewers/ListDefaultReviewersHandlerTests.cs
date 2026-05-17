using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.DefaultReviewers.ListDefaultReviewers;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Repos.DefaultReviewers;

public class ListDefaultReviewersHandlerTests
{
    [Fact]
    public async Task HandleAsync_lists_from_default_reviewers_endpoint()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"uuid":"{u1}","account_id":"a1","display_name":"Alice","type":"user"},
                {"uuid":"{u2}","account_id":"a2","display_name":"Bob","type":"user"}
            ],"next":null}
            """);

        var result = (dynamic)await handler.HandleAsync(
            new ListDefaultReviewersRequest("ws", "myrepo", 25), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/default-reviewers");
        ((int)result.count).Should().Be(2);
    }

    private static ListDefaultReviewersHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new ListDefaultReviewersHandler(client, credentials);
    }
}
