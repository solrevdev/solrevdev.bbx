using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.Hooks.UpdateRepoHook;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Repos.Hooks;

public class UpdateRepoHookHandlerTests
{
    [Fact]
    public async Task HandleAsync_puts_only_the_supplied_fields()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK,
            """{"uuid":"{u1}","description":"d","url":"https://e","active":false,"events":["repo:push"]}""");

        await handler.HandleAsync(
            new UpdateRepoHookRequest("ws", "myrepo", "{u1}", null, null, null, false),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/hooks/%7Bu1%7D");
        http.Calls.Single().Method.Should().Be(HttpMethod.Put);
        var bodyJson = JsonDocument.Parse(http.CallBodies.Single()!);
        bodyJson.RootElement.GetProperty("active").GetBoolean().Should().BeFalse();
        bodyJson.RootElement.TryGetProperty("url", out _).Should().BeFalse();
        bodyJson.RootElement.TryGetProperty("description", out _).Should().BeFalse();
        bodyJson.RootElement.TryGetProperty("events", out _).Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_throws_when_no_field_supplied()
    {
        var handler = BuildHandler(out _,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });

        var act = async () => await handler.HandleAsync(
            new UpdateRepoHookRequest("ws", "myrepo", "{u1}", null, null, null, null),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("Error: Provide at least one of *");
    }

    private static UpdateRepoHookHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new UpdateRepoHookHandler(client, credentials);
    }
}
