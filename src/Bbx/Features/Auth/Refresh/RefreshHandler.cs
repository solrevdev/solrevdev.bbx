using Bbx.Auth;

namespace Bbx.Features.Auth.Refresh;

public sealed class RefreshHandler(CredentialManager credentials, OAuthAuthProvider oauth)
{
    public async Task<string> HandleAsync(RefreshRequest request, CancellationToken ct)
    {
        var config = credentials.LoadConfig();
        if (config.AuthMethod != "oauth" || string.IsNullOrEmpty(config.RefreshToken))
        {
            throw new BbxUserException(
                "Error: bbx auth refresh only applies to OAuth login. Current method: " +
                (string.IsNullOrEmpty(config.AuthMethod) ? "none" : config.AuthMethod));
        }

        var expiresAt = await oauth.ForceRefreshAsync(ct);
        var rel = RelativeTime.DescribeFuture(expiresAt);
        return $"✓ Token refreshed; expires_at: {expiresAt:O} ({rel})";
    }
}
