using System.Net.Http.Headers;
using System.Text.Json;
using Bbx.Auth;

namespace Bbx.Features.Auth.LoginOAuth;

public sealed class LoginOAuthHandler(
    CredentialManager credentials,
    OAuthFlow flow,
    HttpClient http)
{
    public async Task<string> HandleAsync(LoginOAuthRequest request, CancellationToken ct)
    {
        var config = credentials.LoadConfig();

        var clientId = !string.IsNullOrWhiteSpace(request.ClientId)
            ? request.ClientId
            : config.OAuthClientId;
        var clientSecret = !string.IsNullOrWhiteSpace(request.ClientSecret)
            ? request.ClientSecret
            : config.OAuthClientSecret;

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new BbxUserException(
                "Error: OAuth consumer not configured. Run: bbx auth setup-oauth, then re-run with --client-id and --client-secret.");
        }

        OAuthTokenResult token;
        try
        {
            token = await flow.RunAsync(new OAuthFlowOptions
            {
                ClientId = clientId!,
                ClientSecret = clientSecret!,
                Port = request.Port,
                NoBrowser = request.NoBrowser,
                Scopes = request.Scopes,
            }, ct);
        }
        catch (BbxUserException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BbxUserException($"Error: OAuth login failed - {ex.Message}");
        }

        config.AuthMethod = "oauth";
        config.OAuthClientId = clientId;
        config.OAuthClientSecret = clientSecret;
        config.AccessToken = token.AccessToken;
        config.RefreshToken = token.RefreshToken;
        config.TokenExpiry = token.ExpiresAt;
        credentials.SaveConfig(config);

        string displayName = "Unknown";
        string? username = config.Username;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.bitbucket.org/2.0/user");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var resp = await http.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                var user = JsonSerializer.Deserialize<JsonElement>(body);
                if (user.ValueKind == JsonValueKind.Object)
                {
                    if (user.TryGetProperty("display_name", out var dn) && dn.ValueKind == JsonValueKind.String)
                        displayName = dn.GetString() ?? displayName;
                    if (user.TryGetProperty("username", out var un) && un.ValueKind == JsonValueKind.String)
                        username = un.GetString();
                }
            }
        }
        catch
        {
            // Verification is best-effort: tokens are already saved. Surface a
            // generic display name rather than failing the login.
        }

        if (!string.IsNullOrEmpty(username))
        {
            config.Username = username;
            if (string.IsNullOrEmpty(config.DefaultWorkspace))
                config.DefaultWorkspace = username;
            credentials.SaveConfig(config);
        }

        var rel = RelativeTime.DescribeFuture(token.ExpiresAt);
        return $"✓ Authenticated as {displayName} (token expires {rel})";
    }
}
