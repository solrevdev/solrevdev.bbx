using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bbx.Auth;

public sealed class OAuthAuthProvider : IAuthProvider, IDisposable
{
    // 60-second safety margin: refresh just before the token actually expires
    // so we don't hand out a token that flips during a request.
    private static readonly TimeSpan RefreshSafetyMargin = TimeSpan.FromSeconds(60);

    private readonly CredentialManager _credentials;
    private readonly HttpClient _tokenClient;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;
    private bool _loaded;

    public OAuthAuthProvider(CredentialManager credentials, HttpClient tokenClient)
    {
        _credentials = credentials;
        _tokenClient = tokenClient;
    }

    public async Task ApplyAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        if (HasUsableCachedToken()) return _accessToken;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            EnsureLoaded();
            if (HasUsableCachedToken()) return _accessToken;
            return await RefreshLockedAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DateTimeOffset> ForceRefreshAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            EnsureLoaded();
            await RefreshLockedAsync(ct).ConfigureAwait(false);
            return _expiresAt;
        }
        finally
        {
            _gate.Release();
        }
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        var config = _credentials.LoadConfig();
        _accessToken = config.AccessToken;
        _refreshToken = config.RefreshToken;
        _expiresAt = config.TokenExpiry ?? DateTimeOffset.MinValue;
        _loaded = true;
    }

    private bool HasUsableCachedToken()
        => !string.IsNullOrEmpty(_accessToken)
           && DateTimeOffset.UtcNow < _expiresAt - RefreshSafetyMargin;

    private async Task<string?> RefreshLockedAsync(CancellationToken ct)
    {
        var config = _credentials.LoadConfig();
        var refreshToken = !string.IsNullOrEmpty(_refreshToken) ? _refreshToken : config.RefreshToken;

        if (string.IsNullOrEmpty(config.OAuthClientId)
            || string.IsNullOrEmpty(config.OAuthClientSecret)
            || string.IsNullOrEmpty(refreshToken))
        {
            throw new BbxUserException(
                "Error: OAuth credentials missing or incomplete. Run: bbx auth login --oauth");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, OAuthFlow.TokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
            }),
        };
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{config.OAuthClientId}:{config.OAuthClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _tokenClient.SendAsync(request, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new BbxUserException(
                $"Error: OAuth token refresh failed (HTTP {(int)response.StatusCode}): {body}");
        }

        var token = JsonSerializer.Deserialize<RefreshResponse>(body)
                    ?? throw new BbxUserException("Error: OAuth token refresh returned an empty body.");
        if (string.IsNullOrEmpty(token.AccessToken))
            throw new BbxUserException("Error: OAuth token refresh response missing access_token.");

        var expiresIn = token.ExpiresIn > 0 ? TimeSpan.FromSeconds(token.ExpiresIn) : TimeSpan.FromHours(2);
        _accessToken = token.AccessToken;
        if (!string.IsNullOrEmpty(token.RefreshToken)) _refreshToken = token.RefreshToken;
        _expiresAt = DateTimeOffset.UtcNow.Add(expiresIn);

        config.AuthMethod = "oauth";
        config.AccessToken = _accessToken;
        config.RefreshToken = _refreshToken;
        config.TokenExpiry = _expiresAt;
        _credentials.SaveConfig(config);

        return _accessToken;
    }

    public void Dispose() => _gate.Dispose();

    private sealed class RefreshResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
    }
}
