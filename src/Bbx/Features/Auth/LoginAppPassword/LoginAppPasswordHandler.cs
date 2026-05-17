using System.Text.Json;
using Bbx.Auth;
using Bbx.Composition;

namespace Bbx.Features.Auth.LoginAppPassword;

public sealed class LoginAppPasswordHandler(CredentialManager credentials)
{
    public async Task<string> HandleAsync(LoginAppPasswordRequest request, CancellationToken ct)
    {
        using var client = ServiceRegistration.CreateLoginClient(request.Username, request.Password);

        JsonElement user;
        try
        {
            user = await client.GetAsync<JsonElement>("/user", ct);
        }
        catch (Exception ex)
        {
            throw new BbxUserException($"Error: Authentication failed - {ex.Message}");
        }

        var displayName = user.TryGetProperty("display_name", out var dn) ? dn.GetString() : request.Username;
        var defaultWorkspace = user.TryGetProperty("username", out var un) ? un.GetString() : request.Username;

        credentials.SaveConfig(new BbxConfig
        {
            Username = request.Username,
            AppPassword = request.Password,
            DefaultWorkspace = defaultWorkspace,
        });

        return $"✓ Authenticated as {displayName}";
    }
}
