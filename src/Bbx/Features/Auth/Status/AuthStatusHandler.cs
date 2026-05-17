using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Features.Auth.Status;

public sealed class AuthStatusHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task HandleAsync(AuthStatusRequest request, CancellationToken ct)
    {
        var config = credentials.LoadConfig();
        if (!credentials.HasCredentials())
        {
            Console.WriteLine("Not authenticated. Run: bbx auth login");
            return;
        }

        var method = ResolveMethod(config);

        try
        {
            var user = await client.GetAsync<JsonElement>("/user", ct);
            var displayName = user.TryGetProperty("display_name", out var dn) ? dn.GetString() : "Unknown";
            var username = user.TryGetProperty("username", out var un) ? un.GetString() : config.Username;

            Console.WriteLine($"✓ Authenticated as: {displayName}");
            Console.WriteLine($"  Username: {username}");
            Console.WriteLine($"  Auth method: {method}");
            if (method == "oauth" && config.TokenExpiry.HasValue)
            {
                var rel = RelativeTime.DescribeFuture(config.TokenExpiry.Value);
                Console.WriteLine($"  Expires at: {config.TokenExpiry.Value:O} ({rel})");
            }
            if (config.DefaultWorkspace != null)
                Console.WriteLine($"  Default workspace: {config.DefaultWorkspace}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error checking status: {ex.Message}");
        }
    }

    private static string ResolveMethod(BbxConfig config)
    {
        if (!string.IsNullOrEmpty(config.AuthMethod)) return config.AuthMethod;
        if (!string.IsNullOrEmpty(config.AccessToken)) return "oauth";
        return "api-token";
    }
}
