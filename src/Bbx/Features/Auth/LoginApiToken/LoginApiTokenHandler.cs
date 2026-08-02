using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bbx.Auth;

namespace Bbx.Features.Auth.LoginApiToken;

public sealed class LoginApiTokenHandler(CredentialManager credentials, HttpClient http)
{
    public async Task<string> HandleAsync(LoginApiTokenRequest request, CancellationToken ct)
    {
        JsonElement user;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.bitbucket.org/2.0/user");
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{request.Email}:{request.ApiToken}"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var resp = await http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync(ct);
            user = JsonSerializer.Deserialize<JsonElement>(body);
        }
        catch (Exception ex)
        {
            throw new BbxUserException($"Error: Authentication failed - {ex.Message}");
        }

        var displayName = user.TryGetProperty("display_name", out var dn) ? dn.GetString() : "Unknown";
        var username = user.TryGetProperty("username", out var un) ? un.GetString() : null;

        // Keep a default workspace the user has already chosen. Overwriting it
        // with the account name silently retargeted every -w-less command at the
        // personal workspace after re-authenticating.
        var existing = credentials.LoadConfig().DefaultWorkspace;

        credentials.SaveConfig(new BbxConfig
        {
            AuthMethod = "api-token",
            Username = request.Email,
            ApiToken = request.ApiToken,
            DefaultWorkspace = string.IsNullOrEmpty(existing) ? username : existing,
        });

        return $"✓ Authenticated as {displayName}";
    }
}
