using System.CommandLine;
using Bbx.Features.Auth.LoginApiToken;
using Bbx.Features.Auth.LoginAppPassword;
using Bbx.Features.Auth.LoginGuide;
using Bbx.Features.Auth.Logout;
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

                await CommandRunner.RunActionAsync(() =>
                    services.GetRequiredService<LoginApiTokenHandler>()
                        .HandleAsync(new LoginApiTokenRequest(email, token), CancellationToken.None));
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

                await CommandRunner.RunActionAsync(() =>
                    services.GetRequiredService<LoginAppPasswordHandler>()
                        .HandleAsync(new LoginAppPasswordRequest(username, password), CancellationToken.None));
            }
            else
            {
                await services.GetRequiredService<LoginGuideHandler>()
                    .HandleAsync(new LoginGuideRequest(), CancellationToken.None);
            }
        }, appPasswordOption, apiTokenOption);
        command.AddCommand(loginCommand);

        var statusCommand = new Command("status", "Show authentication status");
        statusCommand.SetHandler(async () =>
        {
            await services.GetRequiredService<AuthStatusHandler>()
                .HandleAsync(new AuthStatusRequest(), CancellationToken.None);
        });
        command.AddCommand(statusCommand);

        var logoutCommand = new Command("logout", "Clear stored credentials");
        logoutCommand.SetHandler(async () =>
        {
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<LogoutHandler>()
                    .HandleAsync(new LogoutRequest(), CancellationToken.None));
        });
        command.AddCommand(logoutCommand);

        var tokenCommand = new Command("token", "Display current access token");
        tokenCommand.SetHandler(async () =>
        {
            await services.GetRequiredService<AuthTokenHandler>()
                .HandleAsync(new AuthTokenRequest(), CancellationToken.None);
        });
        command.AddCommand(tokenCommand);

        var setWorkspaceCommand = new Command("set-workspace", "Set default workspace");
        var workspaceArg = new Argument<string>("workspace", "Workspace slug to set as default");
        setWorkspaceCommand.AddArgument(workspaceArg);
        setWorkspaceCommand.SetHandler(async (string workspace) =>
        {
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<SetWorkspaceHandler>()
                    .HandleAsync(new SetWorkspaceRequest(workspace), CancellationToken.None));
        }, workspaceArg);
        command.AddCommand(setWorkspaceCommand);

        return command;
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
