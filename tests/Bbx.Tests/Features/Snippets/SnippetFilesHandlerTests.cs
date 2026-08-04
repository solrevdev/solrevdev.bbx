using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Snippets.SnippetFiles;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Snippets;

public class SnippetFilesHandlerTests
{
    [Fact]
    public async Task File_path_segments_are_encoded_without_losing_directories()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, "contents", "text/plain");
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        var credentials = new CredentialManager(new InMemoryCredentialStore(new BbxConfig
        {
            DefaultWorkspace = "ws",
            Username = "u",
            ApiToken = "t",
        }));
        var handler = new SnippetFilesHandler(client, credentials);

        await handler.HandleAsync(
            new SnippetFilesRequest("snippet", "docs/a#b?.txt", "ws", Raw: true),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri.Should().Be(
            "https://api.bitbucket.org/2.0/snippets/ws/snippet/files/docs/a%23b%3F.txt");
    }
}
