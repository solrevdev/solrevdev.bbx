using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Features.Auth.Status;

public sealed class AuthStatusHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(AuthStatusRequest request, CancellationToken ct)
    {
        var config = credentials.LoadConfig();
        if (!credentials.HasCredentials())
        {
            throw new BbxUserException("Not authenticated. Run: bbx auth login");
        }

        var method = ResolveMethod(config);
        var user = await client.GetAsync<JsonElement>("/user", ct);
        var displayName = user.GetStringOrNull("display_name") ?? "Unknown";
        var username = user.GetStringOrNull("username") ?? config.Username;
        var workspace = config.DefaultWorkspace is null
            ? string.Empty
            : $"{Environment.NewLine}  Default workspace: {config.DefaultWorkspace}";

        return $"✓ Authenticated as: {displayName}{Environment.NewLine}"
               + $"  Username: {username}{Environment.NewLine}"
               + $"  Auth method: {method}{workspace}";
    }

    private static string ResolveMethod(BbxConfig config)
        => string.IsNullOrEmpty(config.AuthMethod) ? "api-token" : config.AuthMethod;
}
