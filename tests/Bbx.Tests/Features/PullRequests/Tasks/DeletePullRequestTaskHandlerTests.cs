using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.PullRequests.Tasks.DeletePullRequestTask;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.PullRequests.Tasks;

public class DeletePullRequestTaskHandlerTests
{
    [Fact]
    public async Task HandleAsync_deletes_task_endpoint()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.NoContent, "");

        await handler.HandleAsync(
            new DeletePullRequestTaskRequest("ws", "myrepo", 42, 7), CancellationToken.None);

        http.Calls.Single().Method.Should().Be(HttpMethod.Delete);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pullrequests/42/tasks/7");
    }

    private static DeletePullRequestTaskHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new DeletePullRequestTaskHandler(client, credentials);
    }
}
