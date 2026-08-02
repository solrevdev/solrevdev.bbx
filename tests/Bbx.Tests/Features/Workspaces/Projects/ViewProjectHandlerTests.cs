using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Workspaces.Projects.ViewProject;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Workspaces.Projects;

public class ViewProjectHandlerTests
{
    [Fact]
    public async Task HandleAsync_escapes_project_key_and_hits_workspaces_projects_path()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.OK,
            """{"key":"P/1","name":"Slash Key","is_private":true,"created_on":"2026-01-01"}""");

        var result = (dynamic)await handler.HandleAsync(
            new ViewProjectRequest("ws", "P/1"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/ws/projects/P%2F1");
        ((string)result.key).Should().Be("P/1");
    }

    private static ViewProjectHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new ViewProjectHandler(client, credentials);
    }
}
