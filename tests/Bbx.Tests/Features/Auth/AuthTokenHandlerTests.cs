using System.Net;
using Bbx.Auth;
using Bbx.Features.Auth.Token;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Auth;

[Collection("Console")]
public class AuthTokenHandlerTests
{
    [Fact]
    public async Task OAuth_path_refreshes_when_needed_and_prints_access_token()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "oauth",
            OAuthClientId = "cid",
            OAuthClientSecret = "csec",
            AccessToken = "expired-access",
            RefreshToken = "rtok-1",
            TokenExpiry = DateTimeOffset.UtcNow.AddSeconds(5),
        });
        var creds = new CredentialManager(store);
        var fakeHttp = new FakeHttpMessageHandler();
        fakeHttp.Enqueue(HttpStatusCode.OK,
            """{"access_token":"fresh-access","refresh_token":"rtok-2","expires_in":7200}""");
        using var http = new HttpClient(fakeHttp);
        using var oauth = new OAuthAuthProvider(creds, http);

        var handler = new AuthTokenHandler(creds, oauth);
        var (stdout, _) = await CaptureConsole.RunAsync(() =>
            handler.HandleAsync(new AuthTokenRequest(), CancellationToken.None));

        stdout.Trim().Should().Be("fresh-access");
        fakeHttp.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task ApiToken_path_prints_username_colon_token_without_network()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "api-token",
            Username = "jane@example.com",
            ApiToken = "ATATT123",
        });
        var creds = new CredentialManager(store);
        var fakeHttp = new FakeHttpMessageHandler();
        using var http = new HttpClient(fakeHttp);
        using var oauth = new OAuthAuthProvider(creds, http);

        var handler = new AuthTokenHandler(creds, oauth);
        var (stdout, _) = await CaptureConsole.RunAsync(() =>
            handler.HandleAsync(new AuthTokenRequest(), CancellationToken.None));

        stdout.Trim().Should().Be("jane@example.com:ATATT123");
        fakeHttp.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Not_authenticated_writes_to_stderr_with_no_network()
    {
        var store = new InMemoryCredentialStore();
        var creds = new CredentialManager(store);
        var fakeHttp = new FakeHttpMessageHandler();
        using var http = new HttpClient(fakeHttp);
        using var oauth = new OAuthAuthProvider(creds, http);

        var handler = new AuthTokenHandler(creds, oauth);
        var (stdout, stderr) = await CaptureConsole.RunAsync(() =>
            handler.HandleAsync(new AuthTokenRequest(), CancellationToken.None));

        stdout.Should().BeEmpty();
        stderr.Should().Contain("Not authenticated");
    }
}
