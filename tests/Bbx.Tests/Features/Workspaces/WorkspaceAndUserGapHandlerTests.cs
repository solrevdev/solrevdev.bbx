using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Users.GpgKeys.ListGpgKeys;
using Bbx.Features.Users.ListUserWorkspaceRepositoryPermissions;
using Bbx.Features.Users.ListUserWorkspaces;
using Bbx.Features.Users.ViewUserEmail;
using Bbx.Features.Users.ViewUserWorkspacePermission;
using Bbx.Features.Workspaces.ListWorkspacePullRequests;
using Bbx.Features.Workspaces.ListWorkspaceRepositoryPermissions;
using Bbx.Features.Workspaces.Pipelines.AddWorkspaceVariable;
using Bbx.Features.Workspaces.Pipelines.UpdateWorkspaceVariable;
using Bbx.Features.Workspaces.Pipelines.ViewWorkspaceOidcConfig;
using Bbx.Features.Workspaces.Projects.BranchingModel.ViewProjectBranchingModelSettings;
using Bbx.Features.Workspaces.Projects.UpdateProject;
using Bbx.Features.Workspaces.ViewWorkspaceGpgKey;
using Bbx.Features.Workspaces.ViewWorkspaceMember;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Workspaces;

public class WorkspaceAndUserGapHandlerTests
{
    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    private static JsonElement Body(FakeHttpMessageHandler http)
        => JsonDocument.Parse(http.CallBodies[0]!).RootElement;

    [Fact]
    public async Task Email_view_escapes_the_address()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"email":"jane@example.com","is_confirmed":true}""");

        await new ViewUserEmailHandler(Client(http)).HandleAsync(
            new ViewUserEmailRequest("jane+bbx@example.com"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri.Should()
            .Be("https://api.bitbucket.org/2.0/user/emails/jane%2Bbbx%40example.com");
    }

    // The account-scoped list. /2.0/workspaces, which `workspace list` calls,
    // is 410 Gone.
    [Fact]
    public async Task User_workspaces_reads_the_account_scoped_list()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[{"slug":"acme","name":"Acme"}],"next":null}""");

        var result = (dynamic)await new ListUserWorkspacesHandler(Client(http)).HandleAsync(
            new ListUserWorkspacesRequest(50), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath.Should().Be("/2.0/user/workspaces");
        ((int)result.count).Should().Be(1);
        ((string)result.workspaces[0].slug).Should().Be("acme");
    }

    [Fact]
    public async Task Workspace_permission_reads_the_singular_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"permission":"owner"}""");

        await new ViewUserWorkspacePermissionHandler(Client(http), Creds()).HandleAsync(
            new ViewUserWorkspacePermissionRequest("acme"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath.Should().Be("/2.0/user/workspaces/acme/permission");
    }

    [Fact]
    public async Task Workspace_scoped_repository_permissions_narrow_to_one_workspace()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await new ListUserWorkspaceRepositoryPermissionsHandler(Client(http), Creds()).HandleAsync(
            new ListUserWorkspaceRepositoryPermissionsRequest("acme", 50),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/user/workspaces/acme/permissions/repositories");
    }

    [Fact]
    public async Task Gpg_keys_list_reads_the_user_scoped_collection()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"account_id":"557058:abc","uuid":"{u}"}""");
        http.Enqueue(HttpStatusCode.OK, """{"values":[{"fingerprint":"ABCD","name":"Jane"}],"next":null}""");

        var result = (dynamic)await new ListGpgKeysHandler(Client(http)).HandleAsync(
            new ListGpgKeysRequest(null, 25), TestContext.Current.CancellationToken);

        http.Calls[1].RequestUri!.AbsolutePath.Should().StartWith("/2.0/users/");
        http.Calls[1].RequestUri!.AbsolutePath.Should().EndWith("/gpg-keys");
        ((int)result.count).Should().Be(1);
    }

    [Fact]
    public async Task Workspace_member_reads_the_single_member()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"user":{"display_name":"Jane"}}""");

        await new ViewWorkspaceMemberHandler(Client(http), Creds()).HandleAsync(
            new ViewWorkspaceMemberRequest("acme", "{uuid-1}"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri.Should()
            .Be("https://api.bitbucket.org/2.0/workspaces/acme/members/%7Buuid-1%7D");
    }

    [Fact]
    public async Task Repo_permissions_span_the_workspace_by_default()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await new ListWorkspaceRepositoryPermissionsHandler(Client(http), Creds()).HandleAsync(
            new ListWorkspaceRepositoryPermissionsRequest("acme", null, 50),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/workspaces/acme/permissions/repositories");
    }

    [Fact]
    public async Task Repo_permissions_narrow_to_one_repository()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await new ListWorkspaceRepositoryPermissionsHandler(Client(http), Creds()).HandleAsync(
            new ListWorkspaceRepositoryPermissionsRequest("acme", "myrepo", 50),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/workspaces/acme/permissions/repositories/myrepo");
    }

    [Fact]
    public async Task Workspace_gpg_key_reads_the_settings_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"public_keys":[]}""");

        await new ViewWorkspaceGpgKeyHandler(Client(http), Creds()).HandleAsync(
            new ViewWorkspaceGpgKeyRequest("acme"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/workspaces/acme/settings/gpg/public-key");
    }

    [Fact]
    public async Task Workspace_pull_requests_pass_the_state_as_a_query()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await new ListWorkspacePullRequestsHandler(Client(http), Creds()).HandleAsync(
            new ListWorkspacePullRequestsRequest("acme", "{u}", "open", 25),
            TestContext.Current.CancellationToken);

        var uri = http.Calls.Single().RequestUri!;
        uri.AbsolutePath.Should().Be("/2.0/workspaces/acme/pullrequests/%7Bu%7D");
        uri.Query.Should().Be("?state=OPEN");
    }

    [Fact]
    public async Task Workspace_variables_live_under_pipelines_config()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"v1","key":"K","value":"v"}""");

        await new AddWorkspaceVariableHandler(Client(http), Creds()).HandleAsync(
            new AddWorkspaceVariableRequest("acme", "K", "v", false),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/workspaces/acme/pipelines-config/variables");
        Body(http).GetProperty("type").GetString().Should().Be("pipeline_variable");
    }

    [Fact]
    public async Task Workspace_variable_update_with_nothing_set_fails()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new UpdateWorkspaceVariableHandler(Client(http), Creds()).HandleAsync(
            new UpdateWorkspaceVariableRequest("acme", "v1", null, null, null),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(UpdateWorkspaceVariableHandler.NothingToUpdate);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Workspace_oidc_config_is_workspace_scoped()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"issuer":"https://api.bitbucket.org"}""");

        await new ViewWorkspaceOidcConfigHandler(Client(http), Creds()).HandleAsync(
            new ViewWorkspaceOidcConfigRequest("acme"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath.Should()
            .Be("/2.0/workspaces/acme/pipelines-config/identity/oidc/.well-known/openid-configuration");
    }

    [Fact]
    public async Task Project_update_sends_only_what_changed()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"key":"PROJ"}""");

        await new UpdateProjectHandler(Client(http), Creds()).HandleAsync(
            new UpdateProjectRequest("acme", "PROJ", "New name", null, null, null),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Put);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/workspaces/acme/projects/PROJ");
        var body = Body(http);
        body.GetProperty("name").GetString().Should().Be("New name");
        body.TryGetProperty("description", out _).Should().BeFalse();
        body.TryGetProperty("key", out _).Should().BeFalse();
    }

    // Renaming the key moves every repository in the project, so it goes out
    // only when the caller asked for it by name.
    [Fact]
    public async Task Project_update_renames_the_key_only_on_request()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"key":"NEW"}""");

        await new UpdateProjectHandler(Client(http), Creds()).HandleAsync(
            new UpdateProjectRequest("acme", "PROJ", null, null, null, "NEW"),
            TestContext.Current.CancellationToken);

        Body(http).GetProperty("key").GetString().Should().Be("NEW");
    }

    [Fact]
    public async Task Project_update_with_no_fields_fails_before_calling_the_api()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new UpdateProjectHandler(Client(http), Creds()).HandleAsync(
            new UpdateProjectRequest("acme", "PROJ", null, null, null, null),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(UpdateProjectHandler.NothingToUpdate);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Project_branching_model_settings_read_the_settings_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"development":{"use_mainbranch":true}}""");

        await new ViewProjectBranchingModelSettingsHandler(Client(http), Creds()).HandleAsync(
            new ViewProjectBranchingModelSettingsRequest("acme", "PROJ"),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/workspaces/acme/projects/PROJ/branching-model/settings");
    }

    [Fact]
    public async Task A_missing_project_key_fails_before_any_call_goes_out()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new UpdateProjectHandler(Client(http), Creds()).HandleAsync(
            new UpdateProjectRequest("acme", "", "n", null, null, null),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }
}
