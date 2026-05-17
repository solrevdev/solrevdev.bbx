using Bbx.Auth;

namespace Bbx.Features.Auth.Token;

public sealed class AuthTokenHandler(CredentialManager credentials)
{
    public Task HandleAsync(AuthTokenRequest request, CancellationToken ct)
    {
        var config = credentials.LoadConfig();
        if (config.AccessToken != null)
            Console.WriteLine(config.AccessToken);
        else if (config.ApiToken != null)
            Console.WriteLine($"{config.Username}:{config.ApiToken}");
        else
            Console.Error.WriteLine("Not authenticated");
        return Task.CompletedTask;
    }
}
