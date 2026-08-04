using System.CommandLine;
using Bbx.Auth;
using Bbx.Features.Auth.LoginApiToken;
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

        command.Subcommands.Add(BuildLoginCommand(services));
        command.Subcommands.Add(BuildStatusCommand(services));
        command.Subcommands.Add(BuildLogoutCommand(services));
        command.Subcommands.Add(BuildTokenCommand(services));
        command.Subcommands.Add(BuildSetWorkspaceCommand(services));

        return command;
    }

    private static Command BuildLoginCommand(IServiceProvider services)
    {
        var loginCommand = new Command("login", "Authenticate with an Atlassian API token");

        // Kept as flags so existing scripts and muscle memory still work; the
        // token flow is the only one, so passing it changes nothing.
        var apiTokenOption = new Option<bool>("--api-token") { Description = "Use an Atlassian API token (the only supported method)" };
        var emailOption = new Option<string?>("--email") { Description = "Atlassian account email (prompted for when omitted)" };
        var tokenOption = new Option<string?>("--token")
        { Description = "API token. Prefer omitting it and letting bbx prompt, or pipe it in, so it stays out of your shell history." };

        loginCommand.Options.Add(apiTokenOption);
        loginCommand.Options.Add(emailOption);
        loginCommand.Options.Add(tokenOption);

        loginCommand.SetHandler(async (string? email, string? token) =>
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                Console.Error.WriteLine("Create a token at https://id.atlassian.com/manage-profile/security/api-tokens");
                Console.Error.Write("Email (Atlassian account): ");
                email = Console.ReadLine()?.Trim();
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                Console.Error.Write("API Token: ");
                token = SecretInput.ReadSecret();
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                throw new BbxUserException("Error: Email and API token required");
            }

            await CommandRunner.RunActionNoGateAsync(() =>
                services.GetRequiredService<LoginApiTokenHandler>()
                    .HandleAsync(new LoginApiTokenRequest(email, token), CommandBinding.CancellationToken));
        }, emailOption, tokenOption);

        return loginCommand;
    }

    private static Command BuildStatusCommand(IServiceProvider services)
    {
        var statusCommand = new Command("status", "Show authentication status");
        statusCommand.SetHandler(() => CommandRunner.RunActionNoGateAsync(() =>
            services.GetRequiredService<AuthStatusHandler>()
                .HandleAsync(new AuthStatusRequest(), CommandBinding.CancellationToken)));
        return statusCommand;
    }

    private static Command BuildLogoutCommand(IServiceProvider services)
    {
        var logoutCommand = new Command("logout", "Clear stored credentials");
        logoutCommand.SetHandler(async () =>
        {
            await CommandRunner.RunActionNoGateAsync(() =>
                services.GetRequiredService<LogoutHandler>()
                    .HandleAsync(new LogoutRequest(), CommandBinding.CancellationToken));
        });
        return logoutCommand;
    }

    private static Command BuildTokenCommand(IServiceProvider services)
    {
        var tokenCommand = new Command("token", "Display current access token");
        tokenCommand.SetHandler(() => CommandRunner.RunActionNoGateAsync(() =>
            services.GetRequiredService<AuthTokenHandler>()
                .HandleAsync(new AuthTokenRequest(), CommandBinding.CancellationToken)));
        return tokenCommand;
    }

    private static Command BuildSetWorkspaceCommand(IServiceProvider services)
    {
        var setWorkspaceCommand = new Command("set-workspace", "Set default workspace");
        var workspaceArg = new Argument<string>("workspace") { Description = "Workspace slug to set as default" };
        setWorkspaceCommand.Arguments.Add(workspaceArg);
        setWorkspaceCommand.SetHandler(async (string workspace) =>
        {
            await CommandRunner.RunActionNoGateAsync(() =>
                services.GetRequiredService<SetWorkspaceHandler>()
                    .HandleAsync(new SetWorkspaceRequest(workspace), CommandBinding.CancellationToken));
        }, workspaceArg);
        return setWorkspaceCommand;
    }

}
