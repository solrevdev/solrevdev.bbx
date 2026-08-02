using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Workspaces.Projects.BranchingModel.UpdateProjectBranchingModelSettings;
using Bbx.Features.Workspaces.Projects.BranchingModel.ViewProjectBranchingModel;
using Bbx.Features.Workspaces.Projects.DefaultReviewers.AddProjectDefaultReviewer;
using Bbx.Features.Workspaces.Projects.DefaultReviewers.ListProjectDefaultReviewers;
using Bbx.Features.Workspaces.Projects.DefaultReviewers.RemoveProjectDefaultReviewer;
using Bbx.Features.Workspaces.Projects.DeployKeys.AddProjectDeployKey;
using Bbx.Features.Workspaces.Projects.DeployKeys.DeleteProjectDeployKey;
using Bbx.Features.Workspaces.Projects.DeployKeys.ListProjectDeployKeys;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Workspaces.Projects;

public class WorkspaceProjectExtensionsTests
{
    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" }));

    [Fact]
    public async Task ListDefaultReviewers_hits_project_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK,
            """{"values":[{"uuid":"{u1}","display_name":"Alice"}],"next":null}""");
        var handler = new ListProjectDefaultReviewersHandler(Client(http), Creds());

        await handler.HandleAsync(
            new ListProjectDefaultReviewersRequest("ws", "PROJ", 25), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/ws/projects/PROJ/default-reviewers");
    }

    [Fact]
    public async Task AddDefaultReviewer_puts_target_in_path()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"{a1}"}""");
        var handler = new AddProjectDefaultReviewerHandler(Client(http), Creds());

        await handler.HandleAsync(
            new AddProjectDefaultReviewerRequest("ws", "PROJ", "{abc}"), CancellationToken.None);

        http.Calls.Single().Method.Should().Be(HttpMethod.Put);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/ws/projects/PROJ/default-reviewers/%7Babc%7D");
    }

    [Fact]
    public async Task RemoveDefaultReviewer_throws_when_target_missing()
    {
        var http = new FakeHttpMessageHandler();
        var handler = new RemoveProjectDefaultReviewerHandler(Client(http), Creds());

        var act = async () => await handler.HandleAsync(
            new RemoveProjectDefaultReviewerRequest("ws", "PROJ", ""), CancellationToken.None);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*--target*");
    }

    [Fact]
    public async Task ViewProjectBranchingModel_hits_branching_model_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"development":{"name":"main"}}""");
        var handler = new ViewProjectBranchingModelHandler(Client(http), Creds());

        await handler.HandleAsync(
            new ViewProjectBranchingModelRequest("ws", "PROJ"), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/ws/projects/PROJ/branching-model");
    }

    [Fact]
    public async Task UpdateProjectBranchingModelSettings_puts_to_settings_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"development":{"name":"main"}}""");
        var handler = new UpdateProjectBranchingModelSettingsHandler(Client(http), Creds());

        await handler.HandleAsync(
            new UpdateProjectBranchingModelSettingsRequest("ws", "PROJ",
                """{"development":{"name":"main"}}"""),
            CancellationToken.None);

        http.Calls.Single().Method.Should().Be(HttpMethod.Put);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/ws/projects/PROJ/branching-model/settings");
    }

    [Fact]
    public async Task ListDeployKeys_hits_project_deploy_keys_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK,
            """{"values":[{"id":1,"label":"prod"}],"next":null}""");
        var handler = new ListProjectDeployKeysHandler(Client(http), Creds());

        await handler.HandleAsync(
            new ListProjectDeployKeysRequest("ws", "PROJ", 25), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/ws/projects/PROJ/deploy-keys");
    }

    [Fact]
    public async Task AddDeployKey_posts_key()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Created, """{"id":1,"label":"prod"}""");
        var handler = new AddProjectDeployKeyHandler(Client(http), Creds());

        await handler.HandleAsync(
            new AddProjectDeployKeyRequest("ws", "PROJ", "ssh-ed25519 AAAA", "prod"),
            CancellationToken.None);

        http.Calls.Single().Method.Should().Be(HttpMethod.Post);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/ws/projects/PROJ/deploy-keys");
    }

    [Fact]
    public async Task DeleteDeployKey_deletes_keyed_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");
        var handler = new DeleteProjectDeployKeyHandler(Client(http), Creds());

        await handler.HandleAsync(
            new DeleteProjectDeployKeyRequest("ws", "PROJ", 7), CancellationToken.None);

        http.Calls.Single().Method.Should().Be(HttpMethod.Delete);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/workspaces/ws/projects/PROJ/deploy-keys/7");
    }
}
