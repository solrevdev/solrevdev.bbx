using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Downloads.GetDownload;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Downloads;

public class GetDownloadHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_raw_bytes_from_downloads_path()
    {
        var http = new FakeHttpMessageHandler();
        http.EnqueueResponder(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }),
        });
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        var credentials = new CredentialManager(
            new InMemoryCredentialStore(new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" }));
        var handler = new GetDownloadHandler(client, credentials);

        await using var destination = new MemoryStream();
        await handler.HandleAsync(
            new GetDownloadRequest("ws", "myrepo", "release.bin", null),
            destination,
            TestContext.Current.CancellationToken);

        destination.ToArray().Should().Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/downloads/release.bin");
    }

    [Fact]
    public async Task HandleAsync_throws_when_filename_empty()
    {
        var http = new FakeHttpMessageHandler();
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        var credentials = new CredentialManager(
            new InMemoryCredentialStore(new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" }));
        var handler = new GetDownloadHandler(client, credentials);

        var act = async () => await handler.HandleAsync(
            new GetDownloadRequest("ws", "myrepo", "", null),
            Stream.Null,
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*<filename>*");
    }
}
