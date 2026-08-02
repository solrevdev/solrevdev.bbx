using Bbx.Features.Auth.LoginApiToken;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Auth;

public static class AuthGate
{
    // Test seam: production wires IsInteractive to Console.IsInputRedirected
    // plus the BBX_NO_INTERACTIVE opt-out. Tests swap it under
    // [Collection("Console")] to drive the prompt without an actual TTY.
    internal static Func<bool> IsInteractive { get; set; } = DefaultIsInteractive;

    internal static bool DefaultIsInteractive()
        => !Console.IsInputRedirected
           && !string.Equals(Environment.GetEnvironmentVariable("BBX_NO_INTERACTIVE"), "1", StringComparison.Ordinal);

    /// <summary>
    /// Make sure a credential is stored before a command runs, prompting for
    /// one on first use when there is a terminal to prompt at.
    /// </summary>
    public static async Task EnsureAuthenticatedAsync(IServiceProvider services, CancellationToken ct)
    {
        var creds = services.GetRequiredService<CredentialManager>();
        if (creds.HasCredentials()) return;

        if (!IsInteractive())
        {
            throw new BbxUserException(
                "Error: Not authenticated. Run: bbx auth login\n" +
                "  Create a token at https://bitbucket.org/account/settings/api-tokens/");
        }

        Console.Error.WriteLine("No credentials found. Create an API token at:");
        Console.Error.WriteLine("  https://bitbucket.org/account/settings/api-tokens/");
        Console.Error.WriteLine();
        Console.Error.Write("Email (Atlassian account): ");
        var email = Console.ReadLine()?.Trim();
        Console.Error.Write("API Token: ");
        var token = SecretInput.ReadSecret();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            throw new BbxUserException("Error: Email and API token are required.");
        }

        await services.GetRequiredService<LoginApiTokenHandler>()
            .HandleAsync(new LoginApiTokenRequest(email, token), ct);

        // The shared IAuthProvider cached NullAuthProvider before login;
        // invalidate it so the original command picks up the new credential.
        if (services.GetService<IAuthProvider>() is ConfigAuthProvider configAuth)
        {
            configAuth.Invalidate();
        }
    }
}
