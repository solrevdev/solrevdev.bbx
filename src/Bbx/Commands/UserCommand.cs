using System.CommandLine;
using Bbx.Features.Users.ListUserEmails;
using Bbx.Features.Users.ListUserRepositoryPermissions;
using Bbx.Features.Users.ListUserWorkspacePermissions;
using Bbx.Features.Users.SshKeys.AddSshKey;
using Bbx.Features.Users.SshKeys.DeleteSshKey;
using Bbx.Features.Users.SshKeys.ListSshKeys;
using Bbx.Features.Users.SshKeys.ViewSshKey;
using Bbx.Features.Users.ViewUser;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class UserCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("user", "User account, emails, permissions, SSH keys");

        var emailsCommand = new Command("emails", "List your account email addresses");
        var emailsLimitOption = new Option<int>("--limit", () => 25, "Maximum emails to list");
        emailsCommand.AddOption(emailsLimitOption);
        emailsCommand.SetHandler((int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListUserEmailsHandler>()
                    .HandleAsync(new ListUserEmailsRequest(limit), CancellationToken.None)),
            emailsLimitOption);
        command.AddCommand(emailsCommand);

        var permissionsCommand = new Command("permissions",
            "List your workspace and repository permissions");

        var permsWsCommand = new Command("workspaces", "List your workspace memberships and roles");
        var permsWsLimitOption = new Option<int>("--limit", () => 50, "Maximum entries to list");
        permsWsCommand.AddOption(permsWsLimitOption);
        permsWsCommand.SetHandler((int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListUserWorkspacePermissionsHandler>()
                    .HandleAsync(new ListUserWorkspacePermissionsRequest(limit), CancellationToken.None)),
            permsWsLimitOption);
        permissionsCommand.AddCommand(permsWsCommand);

        var permsRepoCommand = new Command("repositories", "List your repository-level permissions");
        var permsRepoLimitOption = new Option<int>("--limit", () => 50, "Maximum entries to list");
        permsRepoCommand.AddOption(permsRepoLimitOption);
        permsRepoCommand.SetHandler((int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListUserRepositoryPermissionsHandler>()
                    .HandleAsync(new ListUserRepositoryPermissionsRequest(limit), CancellationToken.None)),
            permsRepoLimitOption);
        permissionsCommand.AddCommand(permsRepoCommand);

        command.AddCommand(permissionsCommand);

        var viewCommand = new Command("view", "View a user profile (defaults to the authenticated account)");
        var viewUserArg = new Argument<string>("selected-user", () => string.Empty,
            "Account UUID or account ID (defaults to the authenticated account). Usernames are no longer accepted by Bitbucket.");
        viewCommand.AddArgument(viewUserArg);
        viewCommand.SetHandler((string selectedUser) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewUserHandler>()
                    .HandleAsync(new ViewUserRequest(selectedUser), CancellationToken.None)),
            viewUserArg);
        command.AddCommand(viewCommand);

        command.AddCommand(CreateSshKeysCommand(services));

        return command;
    }

    private static Command CreateSshKeysCommand(IServiceProvider services)
    {
        var sshCommand = new Command("ssh-keys", "Manage account SSH keys");
        var userOption = new Option<string?>(["--user", "-u"],
            "User selector (UUID, account ID, username); defaults to 'me'");
        sshCommand.AddGlobalOption(userOption);

        var listCommand = new Command("list", "List SSH keys");
        var listLimitOption = new Option<int>("--limit", () => 25, "Maximum keys to list");
        listCommand.AddOption(listLimitOption);
        listCommand.SetHandler((string? user, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListSshKeysHandler>()
                    .HandleAsync(new ListSshKeysRequest(user ?? "me", limit), CancellationToken.None)),
            userOption, listLimitOption);
        sshCommand.AddCommand(listCommand);

        var viewCommand = new Command("view", "View an SSH key");
        var viewIdArg = new Argument<string>("key-id", "SSH key UUID");
        viewCommand.AddArgument(viewIdArg);
        viewCommand.SetHandler((string? user, string keyId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewSshKeyHandler>()
                    .HandleAsync(new ViewSshKeyRequest(user ?? "me", keyId), CancellationToken.None)),
            userOption, viewIdArg);
        sshCommand.AddCommand(viewCommand);

        var addCommand = new Command("add", "Add an SSH key");
        var addKeyOption = new Option<string>("--key", "Public SSH key body") { IsRequired = true };
        var addLabelOption = new Option<string?>("--label", "Friendly label");
        addCommand.AddOption(addKeyOption);
        addCommand.AddOption(addLabelOption);
        addCommand.SetHandler((string? user, string key, string? label) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddSshKeyHandler>()
                    .HandleAsync(new AddSshKeyRequest(user ?? "me", key, label), CancellationToken.None)),
            userOption, addKeyOption, addLabelOption);
        sshCommand.AddCommand(addCommand);

        var deleteCommand = new Command("delete", "Delete an SSH key");
        var deleteIdArg = new Argument<string>("key-id", "SSH key UUID");
        var yesOption = new Option<bool>("--yes", "Skip confirmation");
        deleteCommand.AddArgument(deleteIdArg);
        deleteCommand.AddOption(yesOption);
        deleteCommand.SetHandler(async (string? user, string keyId, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete SSH key '{keyId}'? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteSshKeyHandler>()
                    .HandleAsync(new DeleteSshKeyRequest(user ?? "me", keyId), CancellationToken.None));
        }, userOption, deleteIdArg, yesOption);
        sshCommand.AddCommand(deleteCommand);

        return sshCommand;
    }
}
