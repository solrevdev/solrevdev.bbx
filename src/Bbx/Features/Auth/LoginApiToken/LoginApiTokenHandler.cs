using System.Text.Json;
using Bbx.Auth;
using Bbx.Composition;

namespace Bbx.Features.Auth.LoginApiToken;

public sealed class LoginApiTokenHandler(CredentialManager credentials)
{
    public async Task<string> HandleAsync(LoginApiTokenRequest request, CancellationToken ct)
    {
        using var client = ServiceRegistration.CreateLoginClient(request.Email, request.ApiToken);

        JsonElement user;
        try
        {
            user = await client.GetAsync<JsonElement>("/user", ct);
        }
        catch (Exception ex)
        {
            throw new BbxUserException($"Error: Authentication failed - {ex.Message}");
        }

        var displayName = user.TryGetProperty("display_name", out var dn) ? dn.GetString() : "Unknown";
        var username = user.TryGetProperty("username", out var un) ? un.GetString() : null;

        credentials.SaveConfig(new BbxConfig
        {
            AuthMethod = "api-token",
            Username = request.Email,
            ApiToken = request.ApiToken,
            DefaultWorkspace = username,
        });

        return $"✓ Authenticated as {displayName}";
    }
}
