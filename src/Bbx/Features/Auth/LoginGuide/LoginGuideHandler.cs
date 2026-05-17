namespace Bbx.Features.Auth.LoginGuide;

public sealed class LoginGuideHandler
{
    public Task HandleAsync(LoginGuideRequest request, CancellationToken ct)
    {
        Console.WriteLine("Use --api-token to authenticate with a Bitbucket API token.");
        Console.WriteLine();
        Console.WriteLine("To create an API Token:");
        Console.WriteLine("1. Go to https://bitbucket.org/account/settings/api-tokens/");
        Console.WriteLine("2. Create a new API token with required scopes");
        Console.WriteLine("3. Run: bbx auth login --api-token");
        Console.WriteLine("4. Enter your Atlassian account email and the API token");
        Console.WriteLine();
        Console.WriteLine("Note: App passwords were deprecated Sept 2025. Use --api-token instead.");
        return Task.CompletedTask;
    }
}
