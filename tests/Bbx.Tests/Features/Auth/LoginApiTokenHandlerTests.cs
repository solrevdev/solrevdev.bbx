using System.Net;
using System.Text;
using Bbx.Auth;
using Bbx.Features.Auth.LoginApiToken;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Auth;

public class LoginApiTokenHandlerTests
{
    private static FakeHttpMessageHandler VerifiedUser()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"display_name":"Jane Doe","username":"jane"}""");
        return http;
    }

    [Fact]
    public async Task Verifies_the_credential_with_basic_auth_before_saving_it()
    {
        var http = VerifiedUser();
        var store = new InMemoryCredentialStore();
        var handler = new LoginApiTokenHandler(new CredentialManager(store), TestHttpClientFactory.Create(http));

        var message = await handler.HandleAsync(
            new LoginApiTokenRequest("jane@example.com", "ATATTsecret"), CancellationToken.None);

        message.Should().Contain("Jane Doe");
        var auth = http.Calls.Single().Headers.Authorization;
        auth!.Scheme.Should().Be("Basic");
        Encoding.UTF8.GetString(Convert.FromBase64String(auth.Parameter!))
            .Should().Be("jane@example.com:ATATTsecret");

        var saved = store.Load();
        saved.AuthMethod.Should().Be("api-token");
        saved.ApiToken.Should().Be("ATATTsecret");
    }

    // Regression: the saved default workspace was overwritten with the account
    // name, silently retargeting every -w-less command after re-authenticating.
    [Fact]
    public async Task Keeps_a_default_workspace_the_user_already_chose()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "api-token",
            Username = "jane@example.com",
            ApiToken = "old",
            DefaultWorkspace = "acme-team",
        });
        var handler = new LoginApiTokenHandler(new CredentialManager(store), TestHttpClientFactory.Create(VerifiedUser()));

        await handler.HandleAsync(
            new LoginApiTokenRequest("jane@example.com", "ATATTnew"), CancellationToken.None);

        store.Load().DefaultWorkspace.Should().Be("acme-team");
        store.Load().ApiToken.Should().Be("ATATTnew");
    }

    [Fact]
    public async Task Falls_back_to_the_account_name_when_no_workspace_is_set_yet()
    {
        var store = new InMemoryCredentialStore();
        var handler = new LoginApiTokenHandler(new CredentialManager(store), TestHttpClientFactory.Create(VerifiedUser()));

        await handler.HandleAsync(
            new LoginApiTokenRequest("jane@example.com", "ATATT"), CancellationToken.None);

        store.Load().DefaultWorkspace.Should().Be("jane");
    }

    [Fact]
    public async Task A_rejected_token_is_not_saved()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Unauthorized, """{"type":"error","error":{"message":"nope"}}""");
        var store = new InMemoryCredentialStore();
        var handler = new LoginApiTokenHandler(new CredentialManager(store), TestHttpClientFactory.Create(http));

        var act = async () => await handler.HandleAsync(
            new LoginApiTokenRequest("jane@example.com", "bad"), CancellationToken.None);

        await act.Should().ThrowAsync<BbxUserException>().WithMessage("*Authentication failed*");
        store.SaveCount.Should().Be(0);
    }
}
