using System.Net;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Workspaces.Projects.CreateProject;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Workspaces.Projects;

public class CreateProjectHandlerTests
{
    [Fact]
    public async Task HandleAsync_posts_payload_with_required_fields()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.Created,
            """{"key":"PROJ","name":"Project","is_private":true,"created_on":"2026-01-01"}""");

        await handler.HandleAsync(
            new CreateProjectRequest("ws", "PROJ", "Project", "desc", true),
            CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/ws/projects");
        var bodyJson = JsonDocument.Parse(http.CallBodies.Single()!);
        bodyJson.RootElement.GetProperty("key").GetString().Should().Be("PROJ");
        bodyJson.RootElement.GetProperty("name").GetString().Should().Be("Project");
        bodyJson.RootElement.GetProperty("is_private").GetBoolean().Should().BeTrue();
        bodyJson.RootElement.GetProperty("description").GetString().Should().Be("desc");
    }

    [Fact]
    public async Task HandleAsync_omits_description_when_null()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.Created,
            """{"key":"PROJ","name":"Project","is_private":false}""");

        await handler.HandleAsync(
            new CreateProjectRequest("ws", "PROJ", "Project", null, false),
            CancellationToken.None);

        var bodyJson = JsonDocument.Parse(http.CallBodies.Single()!);
        bodyJson.RootElement.TryGetProperty("description", out _).Should().BeFalse();
        bodyJson.RootElement.GetProperty("is_private").GetBoolean().Should().BeFalse();
    }

    private static CreateProjectHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new CreateProjectHandler(client, credentials);
    }
}
