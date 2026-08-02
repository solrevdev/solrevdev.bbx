using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Commits.CreateCommitStatus;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Commits;

public class CreateCommitStatusHandlerTests
{
    [Fact]
    public async Task HandleAsync_posts_to_statuses_build_endpoint_and_uppercases_state()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.Created,
            """{"key":"ci","state":"SUCCESSFUL","url":"https://ci/run/1"}""");

        await handler.HandleAsync(
            new CreateCommitStatusRequest("ws", "myrepo", "abc123", "ci",
                "successful", "https://ci/run/1", "Build #1", "All green"),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/commit/abc123/statuses/build");
        var body = JsonDocument.Parse(http.CallBodies.Single()!);
        body.RootElement.GetProperty("key").GetString().Should().Be("ci");
        body.RootElement.GetProperty("state").GetString().Should().Be("SUCCESSFUL");
        body.RootElement.GetProperty("url").GetString().Should().Be("https://ci/run/1");
        body.RootElement.GetProperty("name").GetString().Should().Be("Build #1");
        body.RootElement.GetProperty("description").GetString().Should().Be("All green");
    }

    [Fact]
    public async Task HandleAsync_throws_when_state_invalid()
    {
        var handler = BuildHandler(out _,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });

        var act = async () => await handler.HandleAsync(
            new CreateCommitStatusRequest("ws", "myrepo", "abc", "k", "BOGUS", "https://x", null, null),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*--state must be one of*");
    }

    [Fact]
    public async Task HandleAsync_throws_when_key_missing()
    {
        var handler = BuildHandler(out _,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });

        var act = async () => await handler.HandleAsync(
            new CreateCommitStatusRequest("ws", "myrepo", "abc", "", "SUCCESSFUL", "https://x", null, null),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*--key*");
    }

    private static CreateCommitStatusHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new CreateCommitStatusHandler(client, credentials);
    }
}
