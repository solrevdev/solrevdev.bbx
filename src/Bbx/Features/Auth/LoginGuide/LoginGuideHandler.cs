namespace Bbx.Features.Auth.LoginGuide;

public sealed class LoginGuideHandler
{
    public Task HandleAsync(LoginGuideRequest request, CancellationToken ct)
    {
        Console.WriteLine("Choose an authentication method:");
        Console.WriteLine();
        Console.WriteLine("  --oauth       OAuth 2.0 via browser (recommended)");
        Console.WriteLine("                bbx auth login --oauth");
        Console.WriteLine("                First time? Run: bbx auth setup-oauth");
        Console.WriteLine();
        Console.WriteLine("  --api-token   Atlassian API token (fallback; good for CI / scripts)");
        Console.WriteLine("                bbx auth login --api-token");
        Console.WriteLine("                Create one at: https://bitbucket.org/account/settings/api-tokens/");
        Console.WriteLine();
        Console.WriteLine("App passwords are no longer supported (Bitbucket retires them 2026-06-09).");
        return Task.CompletedTask;
    }
}
