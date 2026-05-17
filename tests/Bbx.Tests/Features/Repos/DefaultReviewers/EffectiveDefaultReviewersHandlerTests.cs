using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.DefaultReviewers.EffectiveDefaultReviewers;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Repos.DefaultReviewers;

public class EffectiveDefaultReviewersHandlerTests
{
    [Fact]
    public async Task HandleAsync_hits_effective_endpoint()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"uuid":"{u1}","display_name":"Alice","reviewer_type":"project"},
                {"uuid":"{u2}","display_name":"Bob","reviewer_type":"repository"}
            ],"next":null}
            """);

        var result = (dynamic)await handler.HandleAsync(
            new EffectiveDefaultReviewersRequest("ws", "myrepo", 25), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/effective-default-reviewers");
        ((int)result.count).Should().Be(2);
    }

    [Fact]
    public async Task HandleAsync_stops_at_limit()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"uuid":"{1}"},{"uuid":"{2}"},{"uuid":"{3}"}
            ],"next":null}
            """);

        var result = (dynamic)await handler.HandleAsync(
            new EffectiveDefaultReviewersRequest("ws", "myrepo", 1), CancellationToken.None);

        ((int)result.count).Should().Be(1);
    }

    private static EffectiveDefaultReviewersHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new EffectiveDefaultReviewersHandler(client, credentials);
    }
}
