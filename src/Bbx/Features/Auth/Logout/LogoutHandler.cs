using Bbx.Auth;

namespace Bbx.Features.Auth.Logout;

public sealed class LogoutHandler(CredentialManager credentials)
{
    public Task<string> HandleAsync(LogoutRequest request, CancellationToken ct)
    {
        credentials.ClearConfig();
        return Task.FromResult("✓ Logged out");
    }
}
