using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Downloads.ListDownloads;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Downloads;

public class ListDownloadsHandlerTests
{
    [Fact]
    public async Task HandleAsync_lists_from_downloads_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"name":"v1.tar.gz","size":1024,"downloads":3,"created_on":"2026-01-01","user":{"display_name":"Jane"}},
                {"name":"v2.tar.gz","size":2048,"downloads":1,"created_on":"2026-02-01","user":{"display_name":"Jane"}}
            ],"next":null}
            """);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        var credentials = new CredentialManager(
            new InMemoryCredentialStore(new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" }));
        var handler = new ListDownloadsHandler(client, credentials);

        var result = (dynamic)await handler.HandleAsync(
            new ListDownloadsRequest("ws", "myrepo", 25), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/downloads");
        ((int)result.count).Should().Be(2);
    }
}
