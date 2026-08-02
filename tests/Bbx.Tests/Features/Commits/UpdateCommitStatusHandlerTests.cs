using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Commits.UpdateCommitStatus;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Commits;

public class UpdateCommitStatusHandlerTests
{
    [Fact]
    public async Task HandleAsync_puts_to_keyed_endpoint_with_only_supplied_fields()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK,
            """{"key":"ci","state":"SUCCESSFUL","url":"https://ci/run/1"}""");

        await handler.HandleAsync(
            new UpdateCommitStatusRequest("ws", "myrepo", "abc123", "ci",
                "successful", null, null, null),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Put);
        call.RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/commit/abc123/statuses/build/ci");
        var body = JsonDocument.Parse(http.CallBodies.Single()!);
        body.RootElement.GetProperty("state").GetString().Should().Be("SUCCESSFUL");
        body.RootElement.TryGetProperty("url", out _).Should().BeFalse();
        body.RootElement.TryGetProperty("name", out _).Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_throws_when_no_field_supplied()
    {
        var handler = BuildHandler(out _,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });

        var act = async () => await handler.HandleAsync(
            new UpdateCommitStatusRequest("ws", "myrepo", "abc", "ci", null, null, null, null),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*Provide at least one*");
    }

    private static UpdateCommitStatusHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new UpdateCommitStatusHandler(client, credentials);
    }
}
