using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Tags.ViewTag;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Tags;

public class ViewTagHandlerTests
{
    [Fact]
    public async Task HandleAsync_escapes_tag_name_in_url()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """{"name":"v1.0.0","target":{"hash":"abcdef"}}""");

        await handler.HandleAsync(new ViewTagRequest("ws", "myrepo", "v1.0.0"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/refs/tags/v1.0.0");
    }

    private static ViewTagHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new ViewTagHandler(client, credentials);
    }
}
