using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Downloads.UploadDownload;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Downloads;

public class UploadDownloadHandlerTests
{
    [Fact]
    public async Task HandleAsync_posts_multipart_with_files_part()
    {
        var local = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(local, new byte[] { 1, 2, 3, 4, 5 }, TestContext.Current.CancellationToken);

            var http = new FakeHttpMessageHandler();
            http.Enqueue(HttpStatusCode.Created, "");
            var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
            var credentials = new CredentialManager(
                new InMemoryCredentialStore(new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" }));
            var handler = new UploadDownloadHandler(client, credentials);

            var result = (dynamic)await handler.HandleAsync(
                new UploadDownloadRequest("ws", "myrepo", local, "release-v1.bin"),
                TestContext.Current.CancellationToken);

            http.Calls.Single().RequestUri!.AbsoluteUri
                .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/downloads");
            var body = http.CallBodies.Single()!;
            body.Should().Contain("name=files").And.Contain("release-v1.bin");
            ((string)result.name).Should().Be("release-v1.bin");
            ((long)result.size).Should().Be(5);
        }
        finally
        {
            File.Delete(local);
        }
    }

    [Fact]
    public async Task HandleAsync_throws_when_local_file_missing()
    {
        var http = new FakeHttpMessageHandler();
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        var credentials = new CredentialManager(
            new InMemoryCredentialStore(new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" }));
        var handler = new UploadDownloadHandler(client, credentials);

        var act = async () => await handler.HandleAsync(
            new UploadDownloadRequest("ws", "myrepo", "/does/not/exist", null),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("Error: File not found:*");
    }
}
