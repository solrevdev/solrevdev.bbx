using Bbx.Auth;

namespace Bbx.Features.Auth.Token;

public sealed class AuthTokenHandler(CredentialManager credentials)
{
    public Task HandleAsync(AuthTokenRequest request, CancellationToken ct)
    {
        var config = credentials.LoadConfig();

        // Printed in the form curl and friends expect for Basic auth.
        if (!string.IsNullOrEmpty(config.ApiToken))
        {
            Console.WriteLine($"{config.Username}:{config.ApiToken}");
        }
        else
        {
            Console.Error.WriteLine("Not authenticated");
            Environment.ExitCode = 1;
        }

        return Task.CompletedTask;
    }
}
