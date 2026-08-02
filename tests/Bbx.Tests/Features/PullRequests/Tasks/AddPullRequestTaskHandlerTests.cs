using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.PullRequests.Tasks.AddPullRequestTask;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.PullRequests.Tasks;

public class AddPullRequestTaskHandlerTests
{
    [Fact]
    public async Task HandleAsync_posts_content_raw_payload()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.Created,
            """{"id":7,"state":"UNRESOLVED","content":{"raw":"please check"}}""");

        await handler.HandleAsync(
            new AddPullRequestTaskRequest("ws", "myrepo", 42, "please check"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pullrequests/42/tasks");
        var body = JsonDocument.Parse(http.CallBodies.Single()!);
        body.RootElement.GetProperty("content").GetProperty("raw").GetString().Should().Be("please check");
    }

    [Fact]
    public async Task HandleAsync_throws_when_content_empty()
    {
        var handler = BuildHandler(out _,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });

        var act = async () => await handler.HandleAsync(
            new AddPullRequestTaskRequest("ws", "myrepo", 42, ""), TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*--content*");
    }

    private static AddPullRequestTaskHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new AddPullRequestTaskHandler(client, credentials);
    }
}
