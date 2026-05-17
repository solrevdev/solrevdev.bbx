using System.Net;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.PullRequests.Tasks.UpdatePullRequestTask;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.PullRequests.Tasks;

public class UpdatePullRequestTaskHandlerTests
{
    [Fact]
    public async Task HandleAsync_puts_state_only_when_state_supplied()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """{"id":7,"state":"RESOLVED"}""");

        await handler.HandleAsync(
            new UpdatePullRequestTaskRequest("ws", "myrepo", 42, 7, null, "resolved"),
            CancellationToken.None);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Put);
        call.RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pullrequests/42/tasks/7");
        var body = JsonDocument.Parse(http.CallBodies.Single()!);
        body.RootElement.GetProperty("state").GetString().Should().Be("RESOLVED");
        body.RootElement.TryGetProperty("content", out _).Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_throws_when_state_invalid()
    {
        var handler = BuildHandler(out _,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });

        var act = async () => await handler.HandleAsync(
            new UpdatePullRequestTaskRequest("ws", "myrepo", 42, 7, null, "PENDING"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*RESOLVED, UNRESOLVED*");
    }

    private static UpdatePullRequestTaskHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new UpdatePullRequestTaskHandler(client, credentials);
    }
}
