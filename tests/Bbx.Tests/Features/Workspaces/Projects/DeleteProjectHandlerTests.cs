using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Workspaces.Projects.DeleteProject;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Workspaces.Projects;

public class DeleteProjectHandlerTests
{
    [Fact]
    public async Task HandleAsync_sends_delete_with_escaped_project_key()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { Username = "u", ApiToken = "t", DefaultWorkspace = "ws" });
        http.Enqueue(HttpStatusCode.NoContent, "");

        var message = await handler.HandleAsync(
            new DeleteProjectRequest("ws", "P/1"), CancellationToken.None);

        http.Calls.Single().Method.Should().Be(HttpMethod.Delete);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/ws/projects/P%2F1");
        message.Should().Contain("P/1").And.Contain("ws");
    }

    private static DeleteProjectHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new DeleteProjectHandler(client, credentials);
    }
}
