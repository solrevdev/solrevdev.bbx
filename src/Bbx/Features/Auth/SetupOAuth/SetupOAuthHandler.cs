using Bbx.Auth;

namespace Bbx.Features.Auth.SetupOAuth;

public sealed class SetupOAuthHandler(CredentialManager credentials, IBrowserLauncher browser)
{
    private const string CallbackUrl = "http://localhost:53682/callback";

    private static readonly string[] RequiredScopes =
    {
        "account",
        "repository",
        "repository:write",
        "repository:admin",
        "pullrequest",
        "pullrequest:write",
        "issue",
        "issue:write",
        "pipeline",
        "pipeline:write",
        "pipeline:variable",
        "webhook",
        "snippet",
        "snippet:write",
        "project",
        "project:write",
    };

    public Task HandleAsync(SetupOAuthRequest request, CancellationToken ct)
    {
        var config = credentials.LoadConfig();
        var workspace = string.IsNullOrEmpty(config.DefaultWorkspace) ? "<your-workspace>" : config.DefaultWorkspace;
        var settingsUrl = $"https://bitbucket.org/{workspace}/workspace/settings/api";

        Console.WriteLine("Bitbucket OAuth consumer setup");
        Console.WriteLine("──────────────────────────────");
        Console.WriteLine();
        Console.WriteLine("1. Open your workspace's API settings:");
        Console.WriteLine($"     {settingsUrl}");
        Console.WriteLine();
        Console.WriteLine("2. Under \"OAuth consumers\", click \"Add consumer\" and fill in:");
        Console.WriteLine("     Name:                       bbx");
        Console.WriteLine($"     Callback URL:               {CallbackUrl}");
        Console.WriteLine("     This is a private consumer: Yes");
        Console.WriteLine("     Permissions (suggested):");
        foreach (var scope in RequiredScopes)
            Console.WriteLine($"       - {scope}");
        Console.WriteLine();
        Console.WriteLine("3. Save the consumer. Copy the Key (client_id) and Secret (client_secret).");
        Console.WriteLine();
        Console.WriteLine("4. Run:");
        Console.WriteLine("     bbx auth login --oauth --client-id <key> --client-secret <secret>");
        Console.WriteLine();
        Console.WriteLine($"The callback URL is fixed at {CallbackUrl}; the consumer must match it exactly.");

        if (request.Open)
        {
            try
            {
                browser.Launch(settingsUrl);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"(Could not open browser: {ex.Message})");
            }
        }

        return Task.CompletedTask;
    }
}
