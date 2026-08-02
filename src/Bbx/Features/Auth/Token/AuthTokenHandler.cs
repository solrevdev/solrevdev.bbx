using Bbx.Auth;

namespace Bbx.Features.Auth.Token;

public sealed class AuthTokenHandler(CredentialManager credentials, OAuthAuthProvider oauth)
{
    public async Task HandleAsync(AuthTokenRequest request, CancellationToken ct)
    {
        var config = credentials.LoadConfig();
        if (config.AuthMethod == "oauth" && !string.IsNullOrEmpty(config.RefreshToken))
        {
            var token = await oauth.GetAccessTokenAsync(ct);
            if (string.IsNullOrEmpty(token))
            {
                Console.Error.WriteLine("Not authenticated");
                return;
            }
            Console.WriteLine(token);
            return;
        }

        if (!string.IsNullOrEmpty(config.ApiToken))
        {
            Console.WriteLine($"{config.Username}:{config.ApiToken}");
            return;
        }

        Console.Error.WriteLine("Not authenticated");
    }
}
