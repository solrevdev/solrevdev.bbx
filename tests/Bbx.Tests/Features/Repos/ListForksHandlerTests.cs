using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.ListForks;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Repos;

public class ListForksHandlerTests
{
    [Fact]
    public async Task HandleAsync_lists_from_forks_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK,
            """{"values":[{"full_name":"otheruser/myrepo","slug":"myrepo","is_private":false}],"next":null}""");
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        var credentials = new CredentialManager(
            new InMemoryCredentialStore(new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" }));
        var handler = new ListForksHandler(client, credentials);

        var result = (dynamic)await handler.HandleAsync(
            new ListForksRequest("ws", "myrepo", 25), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/forks");
        ((int)result.count).Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_throws_when_workspace_or_repo_missing()
    {
        var http = new FakeHttpMessageHandler();
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        var credentials = new CredentialManager(new InMemoryCredentialStore());
        var handler = new ListForksHandler(client, credentials);

        var act = async () => await handler.HandleAsync(
            new ListForksRequest(null, null, 25), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
    }
}
