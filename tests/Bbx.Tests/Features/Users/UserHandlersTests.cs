using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Users.ListUserEmails;
using Bbx.Features.Users.ListUserRepositoryPermissions;
using Bbx.Features.Users.ListUserWorkspacePermissions;
using Bbx.Features.Users.SshKeys.AddSshKey;
using Bbx.Features.Users.SshKeys.DeleteSshKey;
using Bbx.Features.Users.SshKeys.ListSshKeys;
using Bbx.Features.Users.ViewUser;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Users;

public class UserHandlersTests
{
    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "john@solrevdev.com", ApiToken = "t" }));

    [Fact]
    public async Task ListUserEmails_hits_user_emails_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK,
            """{"values":[{"email":"a@b","is_primary":true,"is_confirmed":true}],"next":null}""");
        var handler = new ListUserEmailsHandler(Client(http), Creds());

        await handler.HandleAsync(new ListUserEmailsRequest(25), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/user/emails");
    }

    [Fact]
    public async Task ListUserEmails_throws_when_no_credentials()
    {
        var http = new FakeHttpMessageHandler();
        var creds = new CredentialManager(new InMemoryCredentialStore());
        var handler = new ListUserEmailsHandler(Client(http), creds);

        var act = async () => await handler.HandleAsync(new ListUserEmailsRequest(25), TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*Not authenticated*");
    }

    [Fact]
    public async Task ListUserWorkspacePermissions_hits_workspaces_path()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");
        var handler = new ListUserWorkspacePermissionsHandler(Client(http), Creds());

        await handler.HandleAsync(new ListUserWorkspacePermissionsRequest(50), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/user/permissions/workspaces");
    }

    [Fact]
    public async Task ListUserRepositoryPermissions_hits_repositories_path()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");
        var handler = new ListUserRepositoryPermissionsHandler(Client(http), Creds());

        await handler.HandleAsync(new ListUserRepositoryPermissionsRequest(50), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/user/permissions/repositories");
    }

    [Fact]
    public async Task ViewUser_escapes_selector_in_url()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"display_name":"Alice"}""");
        var handler = new ViewUserHandler(Client(http), Creds());

        await handler.HandleAsync(new ViewUserRequest("{abc-uuid}"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/users/%7Babc-uuid%7D");
    }

    // Bitbucket has no /2.0/users/me alias: it reads "me" as an account name and
    // answers "You cannot administer personal accounts of other users". The
    // handlers resolve the selector through /2.0/user first.
    private const string CurrentUserJson = """{"uuid":"{abc-uuid}","account_id":"557058:x"}""";
    private const string CurrentUserPath = "https://api.bitbucket.org/2.0/users/%7Babc-uuid%7D";

    [Theory]
    [InlineData("me")]
    [InlineData("")]
    public async Task ListSshKeys_resolves_current_user_to_its_uuid(string selector)
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, CurrentUserJson);
        http.Enqueue(HttpStatusCode.OK, """{"values":[{"uuid":"{k1}","label":"laptop"}],"next":null}""");
        var handler = new ListSshKeysHandler(Client(http), Creds());

        await handler.HandleAsync(new ListSshKeysRequest(selector, 25), TestContext.Current.CancellationToken);

        http.Calls[0].RequestUri!.AbsoluteUri.Should().Be("https://api.bitbucket.org/2.0/user");
        http.Calls[1].RequestUri!.AbsoluteUri.Should().Be($"{CurrentUserPath}/ssh-keys");
    }

    [Fact]
    public async Task ListSshKeys_uses_an_explicit_selector_verbatim()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");
        var handler = new ListSshKeysHandler(Client(http), Creds());

        await handler.HandleAsync(new ListSshKeysRequest("{other-uuid}", 25), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/users/%7Bother-uuid%7D/ssh-keys");
    }

    [Fact]
    public async Task AddSshKey_posts_key_with_optional_label()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, CurrentUserJson);
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"{k1}","label":"laptop"}""");
        var handler = new AddSshKeyHandler(Client(http), Creds());

        await handler.HandleAsync(
            new AddSshKeyRequest("me", "ssh-ed25519 AAAA", "laptop"), TestContext.Current.CancellationToken);

        http.Calls[1].Method.Should().Be(HttpMethod.Post);
        http.Calls[1].RequestUri!.AbsoluteUri.Should().Be($"{CurrentUserPath}/ssh-keys");
        http.CallBodies[1]!.Should().Contain("ssh-ed25519 AAAA").And.Contain("laptop");
    }

    [Fact]
    public async Task DeleteSshKey_deletes_keyed_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, CurrentUserJson);
        http.Enqueue(HttpStatusCode.NoContent, "");
        var handler = new DeleteSshKeyHandler(Client(http), Creds());

        await handler.HandleAsync(new DeleteSshKeyRequest("me", "{k1}"), TestContext.Current.CancellationToken);

        http.Calls[1].Method.Should().Be(HttpMethod.Delete);
        http.Calls[1].RequestUri!.AbsoluteUri.Should().Be($"{CurrentUserPath}/ssh-keys/%7Bk1%7D");
    }
}
