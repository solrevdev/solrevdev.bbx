using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.PullRequests.Tasks.ListPullRequestTasks;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.PullRequests.Tasks;

public class ListPullRequestTasksHandlerTests
{
    [Fact]
    public async Task HandleAsync_lists_from_tasks_endpoint()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"id":1,"state":"UNRESOLVED","content":{"raw":"check tests"}},
                {"id":2,"state":"RESOLVED","content":{"raw":"add docs"}}
            ],"next":null}
            """);

        var result = (dynamic)await handler.HandleAsync(
            new ListPullRequestTasksRequest("ws", "myrepo", 42, 50), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pullrequests/42/tasks");
        ((int)result.count).Should().Be(2);
    }

    private static ListPullRequestTasksHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new ListPullRequestTasksHandler(client, credentials);
    }
}
