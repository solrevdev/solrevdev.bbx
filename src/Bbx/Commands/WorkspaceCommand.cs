using System.CommandLine;
using Bbx.Features.Workspaces.ListWorkspaceMembers;
using Bbx.Features.Workspaces.ListWorkspacePermissions;
using Bbx.Features.Workspaces.ListWorkspaces;
using Bbx.Features.Workspaces.ViewWorkspace;
using Bbx.Features.Workspaces.WorkspaceHooks;
using Bbx.Features.Workspaces.WorkspaceProjects;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class WorkspaceCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("workspace", "Manage Bitbucket workspaces");

        command.AddCommand(CreateListCommand(services));
        command.AddCommand(CreateViewCommand(services));
        command.AddCommand(CreateMembersCommand(services));
        command.AddCommand(CreateProjectsCommand(services));
        command.AddCommand(CreatePermissionsCommand(services));
        command.AddCommand(CreateHooksCommand(services));

        return command;
    }

    private static Command CreateListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List workspaces the user belongs to");
        var roleOption = new Option<string?>(["--role", "-r"], "Filter by role: owner, collaborator, member");
        var limitOption = new Option<int>(["--limit", "-l"], () => 25, "Maximum number of workspaces to return");

        command.AddOption(roleOption);
        command.AddOption(limitOption);

        command.SetHandler((string? role, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListWorkspacesHandler>()
                    .HandleAsync(new ListWorkspacesRequest(role, limit), CancellationToken.None)),
            roleOption, limitOption);
        return command;
    }

    private static Command CreateViewCommand(IServiceProvider services)
    {
        var command = new Command("view", "View workspace details");
        var workspaceArg = new Argument<string?>("workspace", () => null, "Workspace slug (uses default if not specified)");
        command.AddArgument(workspaceArg);

        command.SetHandler((string? workspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewWorkspaceHandler>()
                    .HandleAsync(new ViewWorkspaceRequest(workspace), CancellationToken.None)),
            workspaceArg);
        return command;
    }

    private static Command CreateMembersCommand(IServiceProvider services)
    {
        var command = new Command("members", "List workspace members");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (uses default if not specified)");
        var limitOption = new Option<int>(["--limit", "-l"], () => 50, "Maximum number of members to return");

        command.AddOption(workspaceOption);
        command.AddOption(limitOption);

        command.SetHandler((string? workspace, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListWorkspaceMembersHandler>()
                    .HandleAsync(new ListWorkspaceMembersRequest(workspace, limit), CancellationToken.None)),
            workspaceOption, limitOption);
        return command;
    }

    private static Command CreateProjectsCommand(IServiceProvider services)
    {
        var command = new Command("projects", "Manage workspace projects");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (uses default if not specified)");
        var viewOption = new Option<string?>(["--view", "-v"], "View specific project by key");
        var createOption = new Option<string?>(["--create", "-c"], "Create new project with this name");
        var keyOption = new Option<string?>(["--key", "-k"], "Project key (required for create, used for operations)");
        var descriptionOption = new Option<string?>(["--description", "-d"], "Project description (for create)");
        var privateOption = new Option<bool?>(["--private", "-p"], "Make project private (for create)");
        var deleteOption = new Option<bool>(["--delete"], () => false, "Delete the project specified by --key");
        var yesOption = new Option<bool>(["--yes", "-y"], () => false, "Skip confirmation prompt for delete");
        var limitOption = new Option<int>(["--limit", "-l"], () => 25, "Maximum number of projects to return");

        command.AddOption(workspaceOption);
        command.AddOption(viewOption);
        command.AddOption(createOption);
        command.AddOption(keyOption);
        command.AddOption(descriptionOption);
        command.AddOption(privateOption);
        command.AddOption(deleteOption);
        command.AddOption(yesOption);
        command.AddOption(limitOption);

        command.SetHandler(async (context) =>
        {
            var workspace = context.ParseResult.GetValueForOption(workspaceOption);
            var view = context.ParseResult.GetValueForOption(viewOption);
            var create = context.ParseResult.GetValueForOption(createOption);
            var key = context.ParseResult.GetValueForOption(keyOption);
            var description = context.ParseResult.GetValueForOption(descriptionOption);
            var isPrivate = context.ParseResult.GetValueForOption(privateOption);
            var delete = context.ParseResult.GetValueForOption(deleteOption);
            var yes = context.ParseResult.GetValueForOption(yesOption);
            var limit = context.ParseResult.GetValueForOption(limitOption);

            if (delete && !yes && !string.IsNullOrEmpty(key) && !CommandRunner.ConfirmOrCancelStderr($"Delete project '{key}'? [y/N]: "))
                return;

            await CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<WorkspaceProjectsHandler>()
                    .HandleAsync(new WorkspaceProjectsRequest(workspace, view, create, key, description, isPrivate, delete, limit), CancellationToken.None));
        });
        return command;
    }

    private static Command CreatePermissionsCommand(IServiceProvider services)
    {
        var command = new Command("permissions", "View workspace permissions");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (uses default if not specified)");
        var limitOption = new Option<int>(["--limit", "-l"], () => 50, "Maximum number of permissions to return");

        command.AddOption(workspaceOption);
        command.AddOption(limitOption);

        command.SetHandler((string? workspace, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListWorkspacePermissionsHandler>()
                    .HandleAsync(new ListWorkspacePermissionsRequest(workspace, limit), CancellationToken.None)),
            workspaceOption, limitOption);
        return command;
    }

    private static Command CreateHooksCommand(IServiceProvider services)
    {
        var command = new Command("hooks", "Manage workspace webhooks");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (uses default if not specified)");
        var viewOption = new Option<string?>(["--view", "-v"], "View specific webhook by UUID");
        var createOption = new Option<string?>(["--create", "-c"], "Create webhook with this URL");
        var descriptionOption = new Option<string?>(["--description", "-d"], "Webhook description");
        var eventsOption = new Option<string[]?>(["--events", "-e"], "Events to trigger webhook (e.g., repo:push, pullrequest:created)");
        var activeOption = new Option<bool?>(["--active", "-a"], "Whether webhook is active");
        var deleteOption = new Option<string?>(["--delete"], "Delete webhook by UUID");
        var yesOption = new Option<bool>(["--yes", "-y"], () => false, "Skip confirmation prompt for delete");
        var limitOption = new Option<int>(["--limit", "-l"], () => 25, "Maximum number of webhooks to return");

        command.AddOption(workspaceOption);
        command.AddOption(viewOption);
        command.AddOption(createOption);
        command.AddOption(descriptionOption);
        command.AddOption(eventsOption);
        command.AddOption(activeOption);
        command.AddOption(deleteOption);
        command.AddOption(yesOption);
        command.AddOption(limitOption);

        command.SetHandler(async (context) =>
        {
            var workspace = context.ParseResult.GetValueForOption(workspaceOption);
            var view = context.ParseResult.GetValueForOption(viewOption);
            var create = context.ParseResult.GetValueForOption(createOption);
            var description = context.ParseResult.GetValueForOption(descriptionOption);
            var events = context.ParseResult.GetValueForOption(eventsOption);
            var active = context.ParseResult.GetValueForOption(activeOption);
            var deleteUuid = context.ParseResult.GetValueForOption(deleteOption);
            var yes = context.ParseResult.GetValueForOption(yesOption);
            var limit = context.ParseResult.GetValueForOption(limitOption);

            if (!string.IsNullOrEmpty(deleteUuid) && !yes && !CommandRunner.ConfirmOrCancelStderr($"Delete webhook '{deleteUuid}'? [y/N]: "))
                return;

            await CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<WorkspaceHooksHandler>()
                    .HandleAsync(new WorkspaceHooksRequest(workspace, view, create, description, events, active, deleteUuid, limit), CancellationToken.None));
        });
        return command;
    }
}
