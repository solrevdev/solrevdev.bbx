using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Workspaces.Projects.ListProjects;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Workspaces.Projects;

public class ListProjectsHandlerTests
{
    [Fact]
    public async Task HandleAsync_uses_default_workspace_when_not_specified()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "default-ws" });
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await handler.HandleAsync(new ListProjectsRequest(null, 25), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/default-ws/projects");
    }

    [Fact]
    public async Task HandleAsync_stops_at_limit_and_projects_summary()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"key":"PROJ1","name":"Project One","is_private":true,"created_on":"2026-01-01"},
                {"key":"PROJ2","name":"Project Two","is_private":false,"created_on":"2026-02-01"},
                {"key":"PROJ3","name":"Project Three","is_private":true,"created_on":"2026-03-01"}
            ],"next":null}
            """);

        var result = (dynamic)await handler.HandleAsync(new ListProjectsRequest("ws", 2), TestContext.Current.CancellationToken);

        ((string)result.workspace).Should().Be("ws");
        ((int)result.count).Should().Be(2);
    }

    private static ListProjectsHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new ListProjectsHandler(client, credentials);
    }
}
