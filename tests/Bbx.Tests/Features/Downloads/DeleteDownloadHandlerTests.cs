using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Downloads.DeleteDownload;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Downloads;

public class DeleteDownloadHandlerTests
{
    [Fact]
    public async Task HandleAsync_deletes_download_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        var credentials = new CredentialManager(
            new InMemoryCredentialStore(new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" }));
        var handler = new DeleteDownloadHandler(client, credentials);

        var msg = await handler.HandleAsync(
            new DeleteDownloadRequest("ws", "myrepo", "release.bin"), TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Delete);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/downloads/release.bin");
        msg.Should().Contain("release.bin");
    }
}
