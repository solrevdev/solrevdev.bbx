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
        var emailsLimitOption = new Option<int>("--limit") { Description = "Maximum emails to list", DefaultValueFactory = _ => 25 };
        emailsCommand.Options.Add(emailsLimitOption);
        emailsCommand.SetHandler((int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListUserEmailsHandler>()
                    .HandleAsync(new ListUserEmailsRequest(limit), CommandBinding.CancellationToken)),
            emailsLimitOption);
        command.Subcommands.Add(emailsCommand);

        var permissionsCommand = new Command("permissions",
            "List your workspace and repository permissions");

        var permsWsCommand = new Command("workspaces", "List your workspace memberships and roles");
        var permsWsLimitOption = new Option<int>("--limit") { Description = "Maximum entries to list", DefaultValueFactory = _ => 50 };
        permsWsCommand.Options.Add(permsWsLimitOption);
        permsWsCommand.SetHandler((int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListUserWorkspacePermissionsHandler>()
                    .HandleAsync(new ListUserWorkspacePermissionsRequest(limit), CommandBinding.CancellationToken)),
            permsWsLimitOption);
        permissionsCommand.Subcommands.Add(permsWsCommand);

        var permsRepoCommand = new Command("repositories", "List your repository-level permissions");
        var permsRepoLimitOption = new Option<int>("--limit") { Description = "Maximum entries to list", DefaultValueFactory = _ => 50 };
        permsRepoCommand.Options.Add(permsRepoLimitOption);
        permsRepoCommand.SetHandler((int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListUserRepositoryPermissionsHandler>()
                    .HandleAsync(new ListUserRepositoryPermissionsRequest(limit), CommandBinding.CancellationToken)),
            permsRepoLimitOption);
        permissionsCommand.Subcommands.Add(permsRepoCommand);

        command.Subcommands.Add(permissionsCommand);

        var viewCommand = new Command("view", "View a user profile (defaults to the authenticated account)");
        var viewUserArg = new Argument<string>("selected-user")
        {
            Description = "Account UUID or account ID (defaults to the authenticated account). Usernames are no longer accepted by Bitbucket.",
            DefaultValueFactory = _ => string.Empty,
        };
        viewCommand.Arguments.Add(viewUserArg);
        viewCommand.SetHandler((string selectedUser) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewUserHandler>()
                    .HandleAsync(new ViewUserRequest(selectedUser), CommandBinding.CancellationToken)),
            viewUserArg);
        command.Subcommands.Add(viewCommand);

        command.Subcommands.Add(CreateSshKeysCommand(services));

        return command;
    }

    private static Command CreateSshKeysCommand(IServiceProvider services)
    {
        var sshCommand = new Command("ssh-keys", "Manage account SSH keys");
        var userOption = new Option<string?>("--user", "-u")
        {
            Description = "User selector (UUID or account ID); defaults to the authenticated account",
        };
        sshCommand.AddRecursiveOption(userOption);

        var listCommand = new Command("list", "List SSH keys");
        var listLimitOption = new Option<int>("--limit") { Description = "Maximum keys to list", DefaultValueFactory = _ => 25 };
        listCommand.Options.Add(listLimitOption);
        listCommand.SetHandler((string? user, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListSshKeysHandler>()
                    .HandleAsync(new ListSshKeysRequest(user ?? "me", limit), CommandBinding.CancellationToken)),
            userOption, listLimitOption);
        sshCommand.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View an SSH key");
        var viewIdArg = new Argument<string>("key-id") { Description = "SSH key UUID" };
        viewCommand.Arguments.Add(viewIdArg);
        viewCommand.SetHandler((string? user, string keyId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewSshKeyHandler>()
                    .HandleAsync(new ViewSshKeyRequest(user ?? "me", keyId), CommandBinding.CancellationToken)),
            userOption, viewIdArg);
        sshCommand.Subcommands.Add(viewCommand);

        var addCommand = new Command("add", "Add an SSH key");
        var addKeyOption = new Option<string>("--key") { Description = "Public SSH key body", Required = true };
        var addLabelOption = new Option<string?>("--label") { Description = "Friendly label" };
        addCommand.Options.Add(addKeyOption);
        addCommand.Options.Add(addLabelOption);
        addCommand.SetHandler((string? user, string key, string? label) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddSshKeyHandler>()
                    .HandleAsync(new AddSshKeyRequest(user ?? "me", key, label), CommandBinding.CancellationToken)),
            userOption, addKeyOption, addLabelOption);
        sshCommand.Subcommands.Add(addCommand);

        var deleteCommand = new Command("delete", "Delete an SSH key");
        var deleteIdArg = new Argument<string>("key-id") { Description = "SSH key UUID" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        deleteCommand.Arguments.Add(deleteIdArg);
        deleteCommand.Options.Add(yesOption);
        deleteCommand.SetHandler(async (string? user, string keyId, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete SSH key '{keyId}'? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteSshKeyHandler>()
                    .HandleAsync(new DeleteSshKeyRequest(user ?? "me", keyId), CommandBinding.CancellationToken));
        }, userOption, deleteIdArg, yesOption);
        sshCommand.Subcommands.Add(deleteCommand);

        return sshCommand;
    }
}
