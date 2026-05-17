using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Tags.DeleteTag;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Tags;

public class DeleteTagHandlerTests
{
    [Fact]
    public async Task HandleAsync_sends_delete_to_tag_url()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.NoContent, "");

        var message = await handler.HandleAsync(
            new DeleteTagRequest("ws", "myrepo", "v1.0.0"), CancellationToken.None);

        http.Calls.Single().Method.Should().Be(HttpMethod.Delete);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/refs/tags/v1.0.0");
        message.Should().Contain("v1.0.0");
    }

    private static DeleteTagHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new DeleteTagHandler(client, credentials);
    }
}
