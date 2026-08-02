using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Source.CatSource;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Source;

public class CatSourceHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_raw_body()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, "hello world\nline two\n", "text/plain");

        var body = await handler.HandleAsync(
            new CatSourceRequest("ws", "myrepo", "main", "/README.md"), TestContext.Current.CancellationToken);

        body.Should().Be("hello world\nline two\n");
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/src/main/README.md");
    }

    [Fact]
    public async Task HandleAsync_throws_when_path_is_empty()
    {
        var handler = BuildHandler(out _,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });

        var act = async () => await handler.HandleAsync(
            new CatSourceRequest("ws", "myrepo", "main", ""), TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*<path> is required*");
    }

    private static CatSourceHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new CatSourceHandler(client, credentials);
    }
}
