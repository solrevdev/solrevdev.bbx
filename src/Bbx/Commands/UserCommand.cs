using System.CommandLine;
using Bbx.Features.Users.GpgKeys.ListGpgKeys;
using Bbx.Features.Users.GpgKeys.ViewGpgKey;
using Bbx.Features.Users.ListUserWorkspaceRepositoryPermissions;
using Bbx.Features.Users.ListUserWorkspaces;
using Bbx.Features.Users.ViewUserEmail;
using Bbx.Features.Users.ViewUserWorkspacePermission;
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
        var emailOption = new Option<string?>("--email")
        { Description = "Look up one address instead, to see whether it is confirmed and primary" };
        emailsCommand.Options.Add(emailsLimitOption);
        emailsCommand.Options.Add(emailOption);
        emailsCommand.SetHandler((int limit, string? email) =>
            CommandRunner.RunJsonAsync<object>(async () => string.IsNullOrEmpty(email)
                ? await services.GetRequiredService<ListUserEmailsHandler>()
                    .HandleAsync(new ListUserEmailsRequest(limit), CommandBinding.CancellationToken)
                : await services.GetRequiredService<ViewUserEmailHandler>()
                    .HandleAsync(new ViewUserEmailRequest(email), CommandBinding.CancellationToken)),
            emailsLimitOption, emailOption);
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

        var permsOneWsCommand = new Command("workspace",
            "Show your role in one workspace");
        var permsOneWsOption = new Option<string?>("--workspace", "-w")
        { Description = "Workspace slug (uses default if not specified)" };
        permsOneWsCommand.Options.Add(permsOneWsOption);
        permsOneWsCommand.SetHandler((string? workspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewUserWorkspacePermissionHandler>()
                    .HandleAsync(new ViewUserWorkspacePermissionRequest(workspace), CommandBinding.CancellationToken)),
            permsOneWsOption);
        permissionsCommand.Subcommands.Add(permsOneWsCommand);

        var permsWsReposCommand = new Command("workspace-repositories",
            "List your repository permissions within one workspace");
        var permsWsReposWsOption = new Option<string?>("--workspace", "-w")
        { Description = "Workspace slug (uses default if not specified)" };
        var permsWsReposLimitOption = new Option<int>("--limit")
        { Description = "Maximum entries to list", DefaultValueFactory = _ => 50 };
        permsWsReposCommand.Options.Add(permsWsReposWsOption);
        permsWsReposCommand.Options.Add(permsWsReposLimitOption);
        permsWsReposCommand.SetHandler((string? workspace, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListUserWorkspaceRepositoryPermissionsHandler>()
                    .HandleAsync(new ListUserWorkspaceRepositoryPermissionsRequest(workspace, limit), CommandBinding.CancellationToken)),
            permsWsReposWsOption, permsWsReposLimitOption);
        permissionsCommand.Subcommands.Add(permsWsReposCommand);

        command.Subcommands.Add(permissionsCommand);

        // The account-scoped workspace list. `workspace list` reads
        // /2.0/workspaces, which Bitbucket withdrew.
        var workspacesCommand = new Command("workspaces", "List the workspaces your account belongs to");
        var workspacesLimitOption = new Option<int>("--limit")
        { Description = "Maximum workspaces to list", DefaultValueFactory = _ => 50 };
        workspacesCommand.Options.Add(workspacesLimitOption);
        workspacesCommand.SetHandler((int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListUserWorkspacesHandler>()
                    .HandleAsync(new ListUserWorkspacesRequest(limit), CommandBinding.CancellationToken)),
            workspacesLimitOption);
        command.Subcommands.Add(workspacesCommand);

        command.Subcommands.Add(CreateGpgKeysCommand(services));

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

    private static Command CreateGpgKeysCommand(IServiceProvider services)
    {
        // Read-only. Adding and deleting a GPG key would mutate the only real
        // account on this machine, and there is no throwaway user to test
        // against.
        var gpgCommand = new Command("gpg-keys", "List and view account GPG keys");
        var userOption = new Option<string?>("--user")
        { Description = "Account UUID or account ID (defaults to the authenticated account)" };
        gpgCommand.AddRecursiveOption(userOption);

        var listCommand = new Command("list", "List GPG keys");
        var limitOption = new Option<int>("--limit") { Description = "Maximum keys to list", DefaultValueFactory = _ => 25 };
        listCommand.Options.Add(limitOption);
        listCommand.SetHandler((string? user, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListGpgKeysHandler>()
                    .HandleAsync(new ListGpgKeysRequest(user, limit), CommandBinding.CancellationToken)),
            userOption, limitOption);
        gpgCommand.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View a GPG key");
        var fingerprintArg = new Argument<string>("fingerprint") { Description = "Key fingerprint" };
        viewCommand.Arguments.Add(fingerprintArg);
        viewCommand.SetHandler((string? user, string fingerprint) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewGpgKeyHandler>()
                    .HandleAsync(new ViewGpgKeyRequest(user, fingerprint), CommandBinding.CancellationToken)),
            userOption, fingerprintArg);
        gpgCommand.Subcommands.Add(viewCommand);

        return gpgCommand;
    }

}
