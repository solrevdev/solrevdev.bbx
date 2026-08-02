using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.PullRequests.RequestChanges;
using Bbx.Features.PullRequests.UnrequestChanges;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.PullRequests;

public class RequestChangesHandlerTests
{
    [Fact]
    public async Task RequestChanges_posts_to_request_changes_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, "");
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        var credentials = new CredentialManager(
            new InMemoryCredentialStore(new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" }));
        var handler = new RequestChangesHandler(client, credentials);

        var msg = await handler.HandleAsync(
            new RequestChangesRequest("ws", "myrepo", 42), TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Post);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pullrequests/42/request-changes");
        msg.Should().Contain("PR #42");
    }

    [Fact]
    public async Task UnrequestChanges_deletes_request_changes_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        var credentials = new CredentialManager(
            new InMemoryCredentialStore(new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" }));
        var handler = new UnrequestChangesHandler(client, credentials);

        await handler.HandleAsync(new UnrequestChangesRequest("ws", "myrepo", 42), TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Delete);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pullrequests/42/request-changes");
    }
}
