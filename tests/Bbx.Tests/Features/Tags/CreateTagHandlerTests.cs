using System.Net;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Tags.CreateTag;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Tags;

public class CreateTagHandlerTests
{
    [Fact]
    public async Task HandleAsync_posts_lightweight_tag_when_no_message()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.Created, """{"name":"v1.0.0","target":{"hash":"abcdef"}}""");

        await handler.HandleAsync(
            new CreateTagRequest("ws", "myrepo", "v1.0.0", "abcdef", null),
            CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/refs/tags");
        var body = JsonDocument.Parse(http.CallBodies.Single()!);
        body.RootElement.GetProperty("name").GetString().Should().Be("v1.0.0");
        body.RootElement.GetProperty("target").GetProperty("hash").GetString().Should().Be("abcdef");
        body.RootElement.TryGetProperty("message", out _).Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_posts_annotated_tag_when_message_supplied()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.Created, """{"name":"v1.0.0"}""");

        await handler.HandleAsync(
            new CreateTagRequest("ws", "myrepo", "v1.0.0", "abcdef", "Release 1.0.0"),
            CancellationToken.None);

        var body = JsonDocument.Parse(http.CallBodies.Single()!);
        body.RootElement.GetProperty("message").GetString().Should().Be("Release 1.0.0");
    }

    private static CreateTagHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new CreateTagHandler(client, credentials);
    }
}
