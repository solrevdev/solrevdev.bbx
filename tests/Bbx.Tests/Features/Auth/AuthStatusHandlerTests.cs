using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Auth.Status;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Auth;

[Collection("Console")]
public class AuthStatusHandlerTests
{
    [Fact]
    public async Task OAuth_status_reports_auth_method_and_expires_at()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "oauth",
            AccessToken = "at",
            RefreshToken = "rt",
            TokenExpiry = expiresAt,
            OAuthClientId = "cid",
            OAuthClientSecret = "csec",
            Username = "jane",
            DefaultWorkspace = "acme",
        });
        var creds = new CredentialManager(store);
        var fakeHttp = new FakeHttpMessageHandler();
        fakeHttp.Enqueue(HttpStatusCode.OK,
            """{"display_name":"Jane Doe","username":"jane"}""");
        using var http = TestHttpClientFactory.Create(fakeHttp);
        using var client = new BitbucketClient(http, new NullAuthProvider());

        var handler = new AuthStatusHandler(client, creds);
        var (stdout, _) = await CaptureConsole.RunAsync(() =>
            handler.HandleAsync(new AuthStatusRequest(), CancellationToken.None));

        stdout.Should().Contain("Auth method: oauth");
        stdout.Should().Contain("Expires at:");
        stdout.Should().Contain("Default workspace: acme");
        stdout.Should().Contain("Jane Doe");
    }

    [Fact]
    public async Task ApiToken_status_reports_api_token_method()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "api-token",
            Username = "jane@example.com",
            ApiToken = "ATATT",
            DefaultWorkspace = "acme",
        });
        var creds = new CredentialManager(store);
        var fakeHttp = new FakeHttpMessageHandler();
        fakeHttp.Enqueue(HttpStatusCode.OK,
            """{"display_name":"Jane Doe","username":"jane"}""");
        using var http = TestHttpClientFactory.Create(fakeHttp);
        using var client = new BitbucketClient(http, new NullAuthProvider());

        var handler = new AuthStatusHandler(client, creds);
        var (stdout, _) = await CaptureConsole.RunAsync(() =>
            handler.HandleAsync(new AuthStatusRequest(), CancellationToken.None));

        stdout.Should().Contain("Auth method: api-token");
        stdout.Should().NotContain("Expires at:");
    }

    [Fact]
    public async Task Reports_not_authenticated_when_no_credentials()
    {
        var store = new InMemoryCredentialStore();
        var creds = new CredentialManager(store);
        var fakeHttp = new FakeHttpMessageHandler();
        using var http = TestHttpClientFactory.Create(fakeHttp);
        using var client = new BitbucketClient(http, new NullAuthProvider());

        var handler = new AuthStatusHandler(client, creds);
        var (stdout, _) = await CaptureConsole.RunAsync(() =>
            handler.HandleAsync(new AuthStatusRequest(), CancellationToken.None));

        stdout.Should().Contain("Not authenticated");
        fakeHttp.Calls.Should().BeEmpty();
    }
}
