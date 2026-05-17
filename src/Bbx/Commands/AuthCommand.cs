using System.CommandLine;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Commands;

public static class AuthCommand
{
    public static Command Create()
    {
        var command = new Command("auth", "Manage authentication");

        // bbx auth login
        var loginCommand = new Command("login", "Authenticate with Bitbucket");
        var appPasswordOption = new Option<bool>("--app-password", "Use app password authentication (deprecated, use --api-token)");
        var apiTokenOption = new Option<bool>("--api-token", "Use API token authentication");
        loginCommand.AddOption(appPasswordOption);
        loginCommand.AddOption(apiTokenOption);
        loginCommand.SetHandler(async (bool useAppPassword, bool useApiToken) =>
        {
            if (useApiToken)
            {
                Console.Write("Email (Atlassian account): ");
                var email = Console.ReadLine()?.Trim();
                Console.Write("API Token: ");
                var token = ReadPassword();

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                {
                    Console.Error.WriteLine("Error: Email and API token required");
                    return;
                }

                // Atlassian API tokens use Basic auth (email:token)
                using var client = new BitbucketClient(appPassword: token, username: email);
                try
                {
                    var user = await client.GetAsync<JsonElement>("/user");
                    var displayName = user.TryGetProperty("display_name", out var dn) ? dn.GetString() : "Unknown";
                    var username = user.TryGetProperty("username", out var un) ? un.GetString() : null;

                    var config = new BbxConfig
                    {
                        Username = email,
                        AppPassword = token,
                        DefaultWorkspace = username
                    };
                    CredentialManager.Save(config);
                    Console.WriteLine($"✓ Authenticated as {displayName}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: Authentication failed - {ex.Message}");
                }
            }
            else if (useAppPassword)
            {
                Console.Write("Username: ");
                var username = Console.ReadLine()?.Trim();
                Console.Write("App Password: ");
                var password = ReadPassword();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    Console.Error.WriteLine("Error: Username and password required");
                    return;
                }

                // Verify credentials
                using var client = new BitbucketClient(appPassword: password, username: username);
                try
                {
                    var user = await client.GetAsync<JsonElement>("/user");
                    var displayName = user.TryGetProperty("display_name", out var dn) ? dn.GetString() : username;
                    var accountId = user.TryGetProperty("account_id", out var ai) ? ai.GetString() : null;

                    var config = new BbxConfig
                    {
                        Username = username,
                        AppPassword = password,
                        DefaultWorkspace = user.TryGetProperty("username", out var un) ? un.GetString() : username
                    };
                    CredentialManager.Save(config);
                    Console.WriteLine($"✓ Authenticated as {displayName}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: Authentication failed - {ex.Message}");
                }
            }
            else
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
            }
        }, appPasswordOption, apiTokenOption);
        command.AddCommand(loginCommand);

        // bbx auth status
        var statusCommand = new Command("status", "Show authentication status");
        statusCommand.SetHandler(async () =>
        {
            var config = CredentialManager.Load();
            if (!CredentialManager.IsAuthenticated())
            {
                Console.WriteLine("Not authenticated. Run: bbx auth login --api-token");
                return;
            }

            using var client = CreateClient(config);
            try
            {
                var user = await client.GetAsync<JsonElement>("/user");
                var displayName = user.TryGetProperty("display_name", out var dn) ? dn.GetString() : "Unknown";
                var username = user.TryGetProperty("username", out var un) ? un.GetString() : config.Username;

                Console.WriteLine($"✓ Authenticated as: {displayName}");
                Console.WriteLine($"  Username: {username}");
                Console.WriteLine($"  Auth method: {((config.ApiToken ?? config.AppPassword) != null ? "API Token / App Password" : "OAuth2")}");
                if (config.DefaultWorkspace != null)
                    Console.WriteLine($"  Default workspace: {config.DefaultWorkspace}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error checking status: {ex.Message}");
            }
        });
        command.AddCommand(statusCommand);

        // bbx auth logout
        var logoutCommand = new Command("logout", "Clear stored credentials");
        logoutCommand.SetHandler(() =>
        {
            CredentialManager.Clear();
            Console.WriteLine("✓ Logged out");
        });
        command.AddCommand(logoutCommand);

        // bbx auth token
        var tokenCommand = new Command("token", "Display current access token");
        tokenCommand.SetHandler(() =>
        {
            var config = CredentialManager.Load();
            var basicSecret = config.ApiToken ?? config.AppPassword;
            if (config.AccessToken != null)
                Console.WriteLine(config.AccessToken);
            else if (basicSecret != null)
                Console.WriteLine($"{config.Username}:{basicSecret}");
            else
                Console.Error.WriteLine("Not authenticated");
        });
        command.AddCommand(tokenCommand);

        // bbx auth set-workspace
        var setWorkspaceCommand = new Command("set-workspace", "Set default workspace");
        var workspaceArg = new Argument<string>("workspace", "Workspace slug to set as default");
        setWorkspaceCommand.AddArgument(workspaceArg);
        setWorkspaceCommand.SetHandler((string workspace) =>
        {
            var config = CredentialManager.Load();
            config.DefaultWorkspace = workspace;
            CredentialManager.Save(config);
            Console.WriteLine($"✓ Default workspace set to: {workspace}");
        }, workspaceArg);
        command.AddCommand(setWorkspaceCommand);

        return command;
    }

    private static BitbucketClient CreateClient(BbxConfig config)
    {
        return new BitbucketClient(
            accessToken: config.AccessToken,
            appPassword: config.ApiToken ?? config.AppPassword,
            username: config.Username);
    }

    private static string ReadPassword()
    {
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine()?.TrimEnd('\r', '\n') ?? string.Empty;
        }

        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
            }
        }
        Console.WriteLine();
        return password.ToString();
    }
}
