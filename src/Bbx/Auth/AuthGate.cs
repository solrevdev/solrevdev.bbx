using Bbx.Features.Auth.LoginOAuth;
using Bbx.Features.Auth.SetupOAuth;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Auth;

public static class AuthGate
{
    // Test seams: production wires IsInteractive to Console.IsInputRedirected
    // + the BBX_NO_INTERACTIVE opt-out, and uses OAuthFlow.DefaultPort for
    // the loopback bind. Tests can swap these under [Collection("Console")]
    // to drive the happy-path auto-launch without an actual TTY.
    internal static Func<bool> IsInteractive { get; set; } = DefaultIsInteractive;
    internal static int LoginPort { get; set; } = OAuthFlow.DefaultPort;

    internal static bool DefaultIsInteractive()
        => !Console.IsInputRedirected
           && !string.Equals(Environment.GetEnvironmentVariable("BBX_NO_INTERACTIVE"), "1", StringComparison.Ordinal);

    public static async Task EnsureAuthenticatedAsync(IServiceProvider services, CancellationToken ct)
    {
        var creds = services.GetRequiredService<CredentialManager>();
        if (creds.HasCredentials()) return;

        if (!IsInteractive())
        {
            throw new BbxUserException(
                "Error: Not authenticated. Run: bbx auth login --oauth (or --api-token).");
        }

        Console.Error.WriteLine("No credentials found — starting OAuth login.");

        var config = creds.LoadConfig();
        var clientId = config.OAuthClientId;
        var clientSecret = config.OAuthClientSecret;

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            await services.GetRequiredService<SetupOAuthHandler>()
                .HandleAsync(new SetupOAuthRequest(Open: false), ct);
            Console.Error.WriteLine();
            Console.Error.Write("client_id (Key from the consumer page): ");
            clientId = Console.ReadLine()?.Trim();
            Console.Error.Write("client_secret (Secret from the consumer page): ");
            clientSecret = SecretInput.ReadSecret();

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new BbxUserException("Error: client_id and client_secret are required.");
            }

            config.OAuthClientId = clientId;
            config.OAuthClientSecret = clientSecret;
            creds.SaveConfig(config);
        }
        else
        {
            Console.Error.WriteLine("Found OAuth consumer in config.");
        }

        var message = await services.GetRequiredService<LoginOAuthHandler>()
            .HandleAsync(new LoginOAuthRequest(clientId, clientSecret, LoginPort, NoBrowser: false, Scopes: null), ct);
        Console.Error.WriteLine(message);

        // The shared IAuthProvider cached NullAuthProvider before login;
        // invalidate it so the original command picks up the new tokens.
        if (services.GetService<IAuthProvider>() is ConfigAuthProvider configAuth)
        {
            configAuth.Invalidate();
        }
    }
}
