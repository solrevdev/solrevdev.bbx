using Bbx.Auth;

namespace Bbx.Features.Auth.Token;

public sealed class AuthTokenHandler(CredentialManager credentials)
{
    public Task<string> HandleAsync(AuthTokenRequest request, CancellationToken ct)
    {
        var config = credentials.LoadConfig();

        // Printed in the form curl and friends expect for Basic auth.
        if (string.IsNullOrEmpty(config.ApiToken))
            throw new BbxUserException("Not authenticated");

        return Task.FromResult($"{config.Username}:{config.ApiToken}");
    }
}
