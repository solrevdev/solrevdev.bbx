using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Pipelines.ViewDeploymentEnvironment;
using Bbx.Features.Repos.RepoPermissions;
using Bbx.Features.Snippets.ViewSnippet;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features;

public class NullNestedObjectHandlerTests
{
    [Fact]
    public async Task ViewSnippet_accepts_null_owner_and_creator()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK,
            """{"id":"s1","owner":null,"creator":null}""");
        var handler = new ViewSnippetHandler(Client(http), Credentials());

        var result = await handler.HandleAsync(
            new ViewSnippetRequest("s1", "ws"), TestContext.Current.CancellationToken);
        var json = System.Text.Json.JsonSerializer.Serialize(result);

        json.Should().Contain("\"owner\":null").And.Contain("\"creator\":null");
    }

    [Fact]
    public async Task ViewDeploymentEnvironment_accepts_null_nested_objects()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK,
            """{"uuid":"e1","environment_type":null,"lock":null}""");
        var handler = new ViewDeploymentEnvironmentHandler(Client(http), Credentials());

        var result = await handler.HandleAsync(
            new ViewDeploymentEnvironmentRequest("ws", "repo", "test"),
            TestContext.Current.CancellationToken);
        var json = System.Text.Json.JsonSerializer.Serialize(result);

        // "lock", not "lock_": lock is a C# keyword, so the property is @lock.
        json.Should().Contain("\"environment_type\":null").And.Contain("\"lock\":null");
    }

    [Fact]
    public async Task RepoPermissions_accepts_a_null_user()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK,
            """{"values":[{"user":null,"permission":"read"}]}""");
        var handler = new RepoPermissionsHandler(Client(http), Credentials());

        var result = await handler.HandleAsync(
            new RepoPermissionsRequest("ws", "repo"), TestContext.Current.CancellationToken);
        var json = System.Text.Json.JsonSerializer.Serialize(result);

        json.Should().Contain("\"user\":null");
    }

    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static CredentialManager Credentials() => new(new InMemoryCredentialStore(new BbxConfig
    {
        DefaultWorkspace = "ws",
        Username = "u",
        ApiToken = "t",
    }));
}
