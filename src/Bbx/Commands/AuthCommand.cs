using System.CommandLine;
using Bbx.Features.Auth.LoginApiToken;
using Bbx.Features.Auth.LoginAppPassword;
using Bbx.Features.Auth.LoginGuide;
using Bbx.Features.Auth.LoginOAuth;
using Bbx.Features.Auth.Logout;
using Bbx.Features.Auth.Refresh;
using Bbx.Features.Auth.SetupOAuth;
using Bbx.Features.Auth.SetWorkspace;
using Bbx.Features.Auth.Status;
using Bbx.Features.Auth.Token;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class AuthCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("auth", "Manage authentication");

        command.AddCommand(BuildLoginCommand(services));
        command.AddCommand(BuildSetupOAuthCommand(services));
        command.AddCommand(BuildRefreshCommand(services));
        command.AddCommand(BuildStatusCommand(services));
        command.AddCommand(BuildLogoutCommand(services));
        command.AddCommand(BuildTokenCommand(services));
        command.AddCommand(BuildSetWorkspaceCommand(services));

        return command;
    }

    private static Command BuildLoginCommand(IServiceProvider services)
    {
        var loginCommand = new Command("login", "Authenticate with Bitbucket");

        var oauthOption = new Option<bool>("--oauth", "Use OAuth 2.0 authorization-code flow (recommended)");
        var appPasswordOption = new Option<bool>("--app-password", "Use app password authentication (deprecated, use --api-token)");
        var apiTokenOption = new Option<bool>("--api-token", "Use API token authentication");
        var clientIdOption = new Option<string?>("--client-id", "OAuth consumer client_id (only with --oauth)");
        var clientSecretOption = new Option<string?>("--client-secret", "OAuth consumer client_secret (only with --oauth)");
        var portOption = new Option<int>("--port", () => 53682, "Loopback port for the OAuth callback (only with --oauth)");
        var noBrowserOption = new Option<bool>("--no-browser", "Print the OAuth URL instead of opening a browser (only with --oauth)");
        var scopesOption = new Option<string?>("--scopes", "Comma-separated scopes (informational; Bitbucket honours consumer scopes)");

        loginCommand.AddOption(oauthOption);
        loginCommand.AddOption(appPasswordOption);
        loginCommand.AddOption(apiTokenOption);
        loginCommand.AddOption(clientIdOption);
        loginCommand.AddOption(clientSecretOption);
        loginCommand.AddOption(portOption);
        loginCommand.AddOption(noBrowserOption);
        loginCommand.AddOption(scopesOption);

        loginCommand.SetHandler(async (context) =>
        {
            var useOauth = context.ParseResult.GetValueForOption(oauthOption);
            var useAppPassword = context.ParseResult.GetValueForOption(appPasswordOption);
            var useApiToken = context.ParseResult.GetValueForOption(apiTokenOption);
            var clientId = context.ParseResult.GetValueForOption(clientIdOption);
            var clientSecret = context.ParseResult.GetValueForOption(clientSecretOption);
            var port = context.ParseResult.GetValueForOption(portOption);
            var noBrowser = context.ParseResult.GetValueForOption(noBrowserOption);
            var scopes = context.ParseResult.GetValueForOption(scopesOption);

            if (useOauth)
            {
                await CommandRunner.RunActionAsync(() =>
                    services.GetRequiredService<LoginOAuthHandler>()
                        .HandleAsync(new LoginOAuthRequest(clientId, clientSecret, port, noBrowser, scopes), CancellationToken.None));
                return;
            }

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

                await CommandRunner.RunActionAsync(() =>
                    services.GetRequiredService<LoginApiTokenHandler>()
                        .HandleAsync(new LoginApiTokenRequest(email, token), CancellationToken.None));
                return;
            }

            if (useAppPassword)
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

                await CommandRunner.RunActionAsync(() =>
                    services.GetRequiredService<LoginAppPasswordHandler>()
                        .HandleAsync(new LoginAppPasswordRequest(username, password), CancellationToken.None));
                return;
            }

            await services.GetRequiredService<LoginGuideHandler>()
                .HandleAsync(new LoginGuideRequest(), CancellationToken.None);
        });

        return loginCommand;
    }

    private static Command BuildSetupOAuthCommand(IServiceProvider services)
    {
        var setupCommand = new Command("setup-oauth", "Print the Bitbucket OAuth consumer setup walkthrough");
        var openOption = new Option<bool>("--open", "Open the workspace API settings page in the browser");
        setupCommand.AddOption(openOption);
        setupCommand.SetHandler(async (bool open) =>
        {
            await services.GetRequiredService<SetupOAuthHandler>()
                .HandleAsync(new SetupOAuthRequest(open), CancellationToken.None);
        }, openOption);
        return setupCommand;
    }

    private static Command BuildRefreshCommand(IServiceProvider services)
    {
        var refreshCommand = new Command("refresh", "Force an OAuth token refresh and print the new expiry");
        refreshCommand.SetHandler(async () =>
        {
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<RefreshHandler>()
                    .HandleAsync(new RefreshRequest(), CancellationToken.None));
        });
        return refreshCommand;
    }

    private static Command BuildStatusCommand(IServiceProvider services)
    {
        var statusCommand = new Command("status", "Show authentication status");
        statusCommand.SetHandler(async () =>
        {
            await services.GetRequiredService<AuthStatusHandler>()
                .HandleAsync(new AuthStatusRequest(), CancellationToken.None);
        });
        return statusCommand;
    }

    private static Command BuildLogoutCommand(IServiceProvider services)
    {
        var logoutCommand = new Command("logout", "Clear stored credentials");
        logoutCommand.SetHandler(async () =>
        {
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<LogoutHandler>()
                    .HandleAsync(new LogoutRequest(), CancellationToken.None));
        });
        return logoutCommand;
    }

    private static Command BuildTokenCommand(IServiceProvider services)
    {
        var tokenCommand = new Command("token", "Display current access token");
        tokenCommand.SetHandler(async () =>
        {
            await services.GetRequiredService<AuthTokenHandler>()
                .HandleAsync(new AuthTokenRequest(), CancellationToken.None);
        });
        return tokenCommand;
    }

    private static Command BuildSetWorkspaceCommand(IServiceProvider services)
    {
        var setWorkspaceCommand = new Command("set-workspace", "Set default workspace");
        var workspaceArg = new Argument<string>("workspace", "Workspace slug to set as default");
        setWorkspaceCommand.AddArgument(workspaceArg);
        setWorkspaceCommand.SetHandler(async (string workspace) =>
        {
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<SetWorkspaceHandler>()
                    .HandleAsync(new SetWorkspaceRequest(workspace), CancellationToken.None));
        }, workspaceArg);
        return setWorkspaceCommand;
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
