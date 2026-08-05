using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.Access.ListRepoGroupPermissions;
using Bbx.Features.Repos.Access.RemoveRepoUserPermission;
using Bbx.Features.Repos.Access.SetRepoGroupPermission;
using Bbx.Features.Repos.Access.SetRepoUserPermission;
using Bbx.Features.Repos.Access.ViewRepoGroupPermission;
using Bbx.Features.Repos.Access.ViewRepoUserPermission;
using Bbx.Features.Workspaces.Projects.Access.ListProjectUserPermissions;
using Bbx.Features.Workspaces.Projects.Access.RemoveProjectGroupPermission;
using Bbx.Features.Workspaces.Projects.Access.SetProjectGroupPermission;
using Bbx.Features.Workspaces.Projects.Access.ViewProjectUserPermission;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Access;

/// <summary>
/// The permissions-config surface. Granting a permission needs a second
/// Bitbucket account or a group, and neither exists on the machine this was
/// built on, so the write verbs were never exercised against the live API.
/// These are the only check on them.
/// </summary>
public class PermissionsConfigHandlerTests
{
    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    private static JsonElement Body(FakeHttpMessageHandler http)
        => JsonDocument.Parse(http.CallBodies[0]!).RootElement;

    [Fact]
    public async Task Repo_group_list_gets_the_groups_collection()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        var result = (dynamic)await new ListRepoGroupPermissionsHandler(Client(http), Creds()).HandleAsync(
            new ListRepoGroupPermissionsRequest("ws", "repo", 50), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/permissions-config/groups");
        ((int)result.count).Should().Be(0);
    }

    [Fact]
    public async Task Repo_group_view_gets_the_single_grant()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"permission":"read","group":{"slug":"devs","name":"Devs"}}""");

        var result = (dynamic)await new ViewRepoGroupPermissionHandler(Client(http), Creds()).HandleAsync(
            new ViewRepoGroupPermissionRequest("ws", "repo", "devs"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/repositories/ws/repo/permissions-config/groups/devs");
        ((string)result.permission).Should().Be("read");
        ((string)result.group.slug).Should().Be("devs");
    }

    // A group grant carries no "user" and a user grant carries no "group". The
    // shaping has to survive either being absent rather than assume both.
    [Fact]
    public async Task A_user_grant_shapes_without_a_group()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK,
            """{"permission":"admin","user":{"display_name":"Jane","account_id":"557058:abc"}}""");

        var result = (dynamic)await new ViewRepoUserPermissionHandler(Client(http), Creds()).HandleAsync(
            new ViewRepoUserPermissionRequest("ws", "repo", "557058:abc"), TestContext.Current.CancellationToken);

        ((string)result.user.display_name).Should().Be("Jane");
        ((object)result.group).Should().BeNull();
    }

    [Fact]
    public async Task Repo_user_set_puts_the_permission()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"permission":"write"}""");

        await new SetRepoUserPermissionHandler(Client(http), Creds()).HandleAsync(
            new SetRepoUserPermissionRequest("ws", "repo", "557058:abc", "write"),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Put);
        call.RequestUri!.AbsoluteUri.Should()
            .Be("https://api.bitbucket.org/2.0/repositories/ws/repo/permissions-config/users/557058%3Aabc");
        Body(http).GetProperty("permission").GetString().Should().Be("write");
    }

    [Fact]
    public async Task A_permission_is_lower_cased_on_the_way_out()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"permission":"admin"}""");

        await new SetRepoGroupPermissionHandler(Client(http), Creds()).HandleAsync(
            new SetRepoGroupPermissionRequest("ws", "repo", "devs", "ADMIN"),
            TestContext.Current.CancellationToken);

        Body(http).GetProperty("permission").GetString().Should().Be("admin");
    }

    // Bitbucket answers an unknown permission with a bare 400, so the check
    // happens here where the message can name the four that work.
    [Fact]
    public async Task An_unknown_permission_fails_before_calling_the_api()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new SetRepoGroupPermissionHandler(Client(http), Creds()).HandleAsync(
            new SetRepoGroupPermissionRequest("ws", "repo", "devs", "owner"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }

    // Projects allow create-repo, repositories do not.
    [Fact]
    public async Task Create_repo_is_a_project_permission_only()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"permission":"create-repo"}""");

        await new SetProjectGroupPermissionHandler(Client(http), Creds()).HandleAsync(
            new SetProjectGroupPermissionRequest("ws", "PROJ", "devs", "create-repo"),
            TestContext.Current.CancellationToken);
        Body(http).GetProperty("permission").GetString().Should().Be("create-repo");

        var repoAct = async () => await new SetRepoGroupPermissionHandler(
                Client(new FakeHttpMessageHandler()), Creds())
            .HandleAsync(new SetRepoGroupPermissionRequest("ws", "repo", "devs", "create-repo"),
                TestContext.Current.CancellationToken);
        await repoAct.Should().ThrowAsync<BbxUserException>();
    }

    [Fact]
    public async Task Project_user_list_gets_the_project_scoped_collection()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await new ListProjectUserPermissionsHandler(Client(http), Creds()).HandleAsync(
            new ListProjectUserPermissionsRequest("ws", "PROJ", 50), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath
            .Should().Be("/2.0/workspaces/ws/projects/PROJ/permissions-config/users");
    }

    [Fact]
    public async Task Project_grants_escape_the_project_key()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"permission":"read"}""");

        await new ViewProjectUserPermissionHandler(Client(http), Creds()).HandleAsync(
            new ViewProjectUserPermissionRequest("ws", "MY PROJ", "557058:abc"),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri.Should()
            .Be("https://api.bitbucket.org/2.0/workspaces/ws/projects/MY%20PROJ/permissions-config/users/557058%3Aabc");
    }

    [Fact]
    public async Task Remove_deletes_the_grant()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");

        await new RemoveProjectGroupPermissionHandler(Client(http), Creds()).HandleAsync(
            new RemoveProjectGroupPermissionRequest("ws", "PROJ", "devs"),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Delete);
        call.RequestUri!.AbsolutePath
            .Should().Be("/2.0/workspaces/ws/projects/PROJ/permissions-config/groups/devs");
    }

    [Fact]
    public async Task A_missing_project_key_fails_before_any_call_goes_out()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new ListProjectUserPermissionsHandler(Client(http), Creds()).HandleAsync(
            new ListProjectUserPermissionsRequest("ws", "", 50), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task A_missing_repository_fails_before_any_call_goes_out()
    {
        var http = new FakeHttpMessageHandler();
        var creds = new CredentialManager(new InMemoryCredentialStore());

        var act = async () => await new RemoveRepoUserPermissionHandler(Client(http), creds).HandleAsync(
            new RemoveRepoUserPermissionRequest(null, null, "557058:abc"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
        http.Calls.Should().BeEmpty();
    }
}
