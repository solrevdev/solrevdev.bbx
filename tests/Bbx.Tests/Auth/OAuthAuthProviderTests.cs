using System.Net;
using Bbx.Auth;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Auth;

public class OAuthAuthProviderTests
{
    [Fact]
    public async Task Returns_cached_access_token_while_within_safety_margin()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "oauth",
            OAuthClientId = "cid",
            OAuthClientSecret = "csec",
            AccessToken = "cached-access",
            RefreshToken = "rtok-1",
            TokenExpiry = DateTimeOffset.UtcNow.AddHours(1),
        });
        var creds = new CredentialManager(store);
        var handler = new FakeHttpMessageHandler();
        using var http = new HttpClient(handler);
        using var provider = new OAuthAuthProvider(creds, http);

        var token = await provider.GetAccessTokenAsync(CancellationToken.None);

        token.Should().Be("cached-access");
        handler.Calls.Should().BeEmpty();
        store.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Refreshes_when_token_is_within_safety_margin_and_persists_rotated_refresh_token()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "oauth",
            OAuthClientId = "cid",
            OAuthClientSecret = "csec",
            AccessToken = "old-access",
            RefreshToken = "rtok-1",
            TokenExpiry = DateTimeOffset.UtcNow.AddSeconds(30),
        });
        var creds = new CredentialManager(store);
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK,
            """{"access_token":"new-access","refresh_token":"rtok-2","expires_in":7200}""");
        using var http = new HttpClient(handler);
        using var provider = new OAuthAuthProvider(creds, http);

        var token = await provider.GetAccessTokenAsync(CancellationToken.None);

        token.Should().Be("new-access");
        handler.Calls.Should().ContainSingle();
        var call = handler.Calls[0];
        call.RequestUri.Should().Be(new Uri("https://bitbucket.org/site/oauth2/access_token"));
        call.Method.Should().Be(HttpMethod.Post);
        call.Headers.Authorization.Should().NotBeNull();
        call.Headers.Authorization!.Scheme.Should().Be("Basic");
        var decoded = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(call.Headers.Authorization.Parameter!));
        decoded.Should().Be("cid:csec");
        handler.CallBodies[0].Should().Contain("grant_type=refresh_token");
        handler.CallBodies[0].Should().Contain("refresh_token=rtok-1");

        var snapshot = store.Snapshot();
        snapshot.Should().NotBeNull();
        snapshot!.AccessToken.Should().Be("new-access");
        snapshot.RefreshToken.Should().Be("rtok-2");
        snapshot.AuthMethod.Should().Be("oauth");
        snapshot.TokenExpiry.Should().NotBeNull();
        snapshot.TokenExpiry!.Value.Should().BeCloseTo(
            DateTimeOffset.UtcNow.AddSeconds(7200), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ForceRefreshAsync_refreshes_even_when_token_is_still_fresh()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "oauth",
            OAuthClientId = "cid",
            OAuthClientSecret = "csec",
            AccessToken = "fresh-access",
            RefreshToken = "rtok-1",
            TokenExpiry = DateTimeOffset.UtcNow.AddHours(1),
        });
        var creds = new CredentialManager(store);
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK,
            """{"access_token":"forced-access","refresh_token":"rtok-2","expires_in":7200}""");
        using var http = new HttpClient(handler);
        using var provider = new OAuthAuthProvider(creds, http);

        var expiresAt = await provider.ForceRefreshAsync(CancellationToken.None);

        expiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddSeconds(7200), TimeSpan.FromSeconds(5));
        handler.Calls.Should().ContainSingle();
        store.Snapshot()!.AccessToken.Should().Be("forced-access");
    }

    [Fact]
    public async Task Applies_bearer_header_on_outgoing_request()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "oauth",
            OAuthClientId = "cid",
            OAuthClientSecret = "csec",
            AccessToken = "cached-access",
            RefreshToken = "rtok-1",
            TokenExpiry = DateTimeOffset.UtcNow.AddHours(1),
        });
        var creds = new CredentialManager(store);
        var handler = new FakeHttpMessageHandler();
        using var http = new HttpClient(handler);
        using var provider = new OAuthAuthProvider(creds, http);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.bitbucket.org/2.0/user");
        await provider.ApplyAsync(request, CancellationToken.None);

        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be("cached-access");
    }

    [Fact]
    public async Task Concurrent_callers_share_a_single_refresh()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "oauth",
            OAuthClientId = "cid",
            OAuthClientSecret = "csec",
            AccessToken = "stale",
            RefreshToken = "rtok-1",
            TokenExpiry = DateTimeOffset.UtcNow.AddSeconds(5),
        });
        var creds = new CredentialManager(store);
        var handler = new FakeHttpMessageHandler();

        var gate = new TaskCompletionSource();
        handler.EnqueueResponder(_ =>
        {
            gate.Task.Wait();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"access_token":"refreshed-access","refresh_token":"rtok-2","expires_in":7200}""",
                    System.Text.Encoding.UTF8, "application/json"),
            };
        });
        using var http = new HttpClient(handler);
        using var provider = new OAuthAuthProvider(creds, http);

        var t1 = Task.Run(() => provider.GetAccessTokenAsync(CancellationToken.None));
        var t2 = Task.Run(() => provider.GetAccessTokenAsync(CancellationToken.None));
        var t3 = Task.Run(() => provider.GetAccessTokenAsync(CancellationToken.None));

        await Task.Delay(50);
        gate.SetResult();
        var tokens = await Task.WhenAll(t1, t2, t3);

        tokens.Should().AllBe("refreshed-access");
        handler.Calls.Should().ContainSingle();
        store.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Throws_BbxUserException_when_consumer_or_refresh_token_missing()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "oauth",
            OAuthClientId = null,
            OAuthClientSecret = null,
            RefreshToken = null,
            TokenExpiry = DateTimeOffset.UtcNow.AddSeconds(-1),
        });
        var creds = new CredentialManager(store);
        var handler = new FakeHttpMessageHandler();
        using var http = new HttpClient(handler);
        using var provider = new OAuthAuthProvider(creds, http);

        var act = async () => await provider.GetAccessTokenAsync(CancellationToken.None);
        await act.Should().ThrowAsync<BbxUserException>().WithMessage("*OAuth credentials missing*");
        handler.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Throws_BbxUserException_when_refresh_response_is_an_error()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "oauth",
            OAuthClientId = "cid",
            OAuthClientSecret = "csec",
            RefreshToken = "rtok-1",
            TokenExpiry = DateTimeOffset.UtcNow.AddSeconds(-1),
        });
        var creds = new CredentialManager(store);
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""");
        using var http = new HttpClient(handler);
        using var provider = new OAuthAuthProvider(creds, http);

        var act = async () => await provider.GetAccessTokenAsync(CancellationToken.None);
        await act.Should().ThrowAsync<BbxUserException>().WithMessage("*OAuth token refresh failed*HTTP 400*");
    }
}
