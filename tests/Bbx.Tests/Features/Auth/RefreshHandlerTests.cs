using System.Net;
using Bbx.Auth;
using Bbx.Features.Auth.Refresh;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Auth;

public class RefreshHandlerTests
{
    [Fact]
    public async Task Forces_refresh_and_returns_new_expiry_message()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "oauth",
            OAuthClientId = "cid",
            OAuthClientSecret = "csec",
            AccessToken = "old-access",
            RefreshToken = "rtok-1",
            TokenExpiry = DateTimeOffset.UtcNow.AddHours(1),
        });
        var creds = new CredentialManager(store);
        var fakeHttp = new FakeHttpMessageHandler();
        fakeHttp.Enqueue(HttpStatusCode.OK,
            """{"access_token":"new-access","refresh_token":"rtok-2","expires_in":7200}""");
        using var http = new HttpClient(fakeHttp);
        using var oauth = new OAuthAuthProvider(creds, http);

        var handler = new RefreshHandler(creds, oauth);
        var message = await handler.HandleAsync(new RefreshRequest(), CancellationToken.None);

        message.Should().StartWith("✓ Token refreshed");
        fakeHttp.Calls.Should().ContainSingle();
        store.Snapshot()!.AccessToken.Should().Be("new-access");
        store.Snapshot()!.RefreshToken.Should().Be("rtok-2");
    }

    [Fact]
    public async Task Throws_BbxUserException_when_method_is_not_oauth()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "api-token",
            Username = "jane@example.com",
            ApiToken = "ATATT",
        });
        var creds = new CredentialManager(store);
        var fakeHttp = new FakeHttpMessageHandler();
        using var http = new HttpClient(fakeHttp);
        using var oauth = new OAuthAuthProvider(creds, http);

        var handler = new RefreshHandler(creds, oauth);
        var act = async () => await handler.HandleAsync(new RefreshRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage("*only applies to OAuth*api-token*");
        fakeHttp.Calls.Should().BeEmpty();
    }
}
