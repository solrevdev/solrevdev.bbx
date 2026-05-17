using System.CommandLine;
using Bbx.Features.Workspaces.ListWorkspaceMembers;
using Bbx.Features.Workspaces.ListWorkspacePermissions;
using Bbx.Features.Workspaces.ListWorkspaces;
using Bbx.Features.Workspaces.Projects.BranchingModel.UpdateProjectBranchingModelSettings;
using Bbx.Features.Workspaces.Projects.BranchingModel.ViewProjectBranchingModel;
using Bbx.Features.Workspaces.Projects.DefaultReviewers.AddProjectDefaultReviewer;
using Bbx.Features.Workspaces.Projects.DefaultReviewers.ListProjectDefaultReviewers;
using Bbx.Features.Workspaces.Projects.DefaultReviewers.RemoveProjectDefaultReviewer;
using Bbx.Features.Workspaces.Projects.DeployKeys.AddProjectDeployKey;
using Bbx.Features.Workspaces.Projects.DeployKeys.DeleteProjectDeployKey;
using Bbx.Features.Workspaces.Projects.DeployKeys.ListProjectDeployKeys;
using Bbx.Features.Workspaces.Projects.DeployKeys.ViewProjectDeployKey;
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
        command.AddCommand(CreateProjectCommand(services));

        return command;
    }

    private static Command CreateProjectCommand(IServiceProvider services)
    {
        // `project` (singular) is the per-verb shape for the Phase 3 projects
        // sub-API (default reviewers, branching model, deploy keys). The
        // existing `projects` (plural) command keeps its flat-flag shape so
        // we don't drift the read/CRUD path users already use.
        var projectCommand = new Command("project",
            "Per-project settings (default reviewers, branching model, deploy keys)");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (defaults to configured)");
        var projectKeyOption = new Option<string>("--project-key", "Project key") { IsRequired = true };
        projectCommand.AddGlobalOption(workspaceOption);
        projectCommand.AddGlobalOption(projectKeyOption);

        projectCommand.AddCommand(CreateProjectDefaultReviewersCommand(services, workspaceOption, projectKeyOption));
        projectCommand.AddCommand(CreateProjectBranchingModelCommand(services, workspaceOption, projectKeyOption));
        projectCommand.AddCommand(CreateProjectDeployKeysCommand(services, workspaceOption, projectKeyOption));

        return projectCommand;
    }

    private static Command CreateProjectDefaultReviewersCommand(IServiceProvider services, Option<string?> workspaceOption, Option<string> projectKeyOption)
    {
        var drCommand = new Command("default-reviewers", "Manage project-level default reviewers");

        var listCommand = new Command("list", "List project default reviewers");
        var listLimitOption = new Option<int>("--limit", () => 25, "Maximum reviewers to list");
        listCommand.AddOption(listLimitOption);
        listCommand.SetHandler((string? workspace, string projectKey, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListProjectDefaultReviewersHandler>()
                    .HandleAsync(new ListProjectDefaultReviewersRequest(workspace, projectKey, limit), CancellationToken.None)),
            workspaceOption, projectKeyOption, listLimitOption);
        drCommand.AddCommand(listCommand);

        var addCommand = new Command("add", "Add a project default reviewer");
        var addTargetOption = new Option<string>("--target", "Account ID or UUID of the user") { IsRequired = true };
        addCommand.AddOption(addTargetOption);
        addCommand.SetHandler((string? workspace, string projectKey, string target) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddProjectDefaultReviewerHandler>()
                    .HandleAsync(new AddProjectDefaultReviewerRequest(workspace, projectKey, target), CancellationToken.None)),
            workspaceOption, projectKeyOption, addTargetOption);
        drCommand.AddCommand(addCommand);

        var removeCommand = new Command("remove", "Remove a project default reviewer");
        var removeTargetOption = new Option<string>("--target", "Account ID or UUID of the user") { IsRequired = true };
        var yesOption = new Option<bool>("--yes", "Skip confirmation");
        removeCommand.AddOption(removeTargetOption);
        removeCommand.AddOption(yesOption);
        removeCommand.SetHandler(async (string? workspace, string projectKey, string target, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Remove default reviewer '{target}'? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<RemoveProjectDefaultReviewerHandler>()
                    .HandleAsync(new RemoveProjectDefaultReviewerRequest(workspace, projectKey, target), CancellationToken.None));
        }, workspaceOption, projectKeyOption, removeTargetOption, yesOption);
        drCommand.AddCommand(removeCommand);

        return drCommand;
    }

    private static Command CreateProjectBranchingModelCommand(IServiceProvider services, Option<string?> workspaceOption, Option<string> projectKeyOption)
    {
        var bmCommand = new Command("branching-model",
            "Inspect or update the project branching-model defaults");

        var viewCommand = new Command("view", "Show the project branching-model defaults");
        viewCommand.SetHandler((string? workspace, string projectKey) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewProjectBranchingModelHandler>()
                    .HandleAsync(new ViewProjectBranchingModelRequest(workspace, projectKey), CancellationToken.None)),
            workspaceOption, projectKeyOption);
        bmCommand.AddCommand(viewCommand);

        var updateCommand = new Command("update",
            "Replace project branching-model settings (PUT raw JSON payload to /branching-model/settings)");
        var settingsJsonOption = new Option<string>("--settings", "JSON payload") { IsRequired = true };
        updateCommand.AddOption(settingsJsonOption);
        updateCommand.SetHandler((string? workspace, string projectKey, string settingsJson) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateProjectBranchingModelSettingsHandler>()
                    .HandleAsync(new UpdateProjectBranchingModelSettingsRequest(workspace, projectKey, settingsJson), CancellationToken.None)),
            workspaceOption, projectKeyOption, settingsJsonOption);
        bmCommand.AddCommand(updateCommand);

        return bmCommand;
    }

    private static Command CreateProjectDeployKeysCommand(IServiceProvider services, Option<string?> workspaceOption, Option<string> projectKeyOption)
    {
        var dkCommand = new Command("deploy-keys", "Manage project-level deploy keys");

        var listCommand = new Command("list", "List project deploy keys");
        var listLimitOption = new Option<int>("--limit", () => 25, "Maximum keys to list");
        listCommand.AddOption(listLimitOption);
        listCommand.SetHandler((string? workspace, string projectKey, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListProjectDeployKeysHandler>()
                    .HandleAsync(new ListProjectDeployKeysRequest(workspace, projectKey, limit), CancellationToken.None)),
            workspaceOption, projectKeyOption, listLimitOption);
        dkCommand.AddCommand(listCommand);

        var viewCommand = new Command("view", "View a project deploy key");
        var viewIdArg = new Argument<int>("key-id", "Deploy key ID");
        viewCommand.AddArgument(viewIdArg);
        viewCommand.SetHandler((string? workspace, string projectKey, int keyId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewProjectDeployKeyHandler>()
                    .HandleAsync(new ViewProjectDeployKeyRequest(workspace, projectKey, keyId), CancellationToken.None)),
            workspaceOption, projectKeyOption, viewIdArg);
        dkCommand.AddCommand(viewCommand);

        var addCommand = new Command("add", "Add a project deploy key");
        var addKeyOption = new Option<string>("--key", "Public SSH key body") { IsRequired = true };
        var addLabelOption = new Option<string?>("--label", "Friendly label");
        addCommand.AddOption(addKeyOption);
        addCommand.AddOption(addLabelOption);
        addCommand.SetHandler((string? workspace, string projectKey, string key, string? label) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddProjectDeployKeyHandler>()
                    .HandleAsync(new AddProjectDeployKeyRequest(workspace, projectKey, key, label), CancellationToken.None)),
            workspaceOption, projectKeyOption, addKeyOption, addLabelOption);
        dkCommand.AddCommand(addCommand);

        var deleteCommand = new Command("delete", "Delete a project deploy key");
        var deleteIdArg = new Argument<int>("key-id", "Deploy key ID");
        var yesOption = new Option<bool>("--yes", "Skip confirmation");
        deleteCommand.AddArgument(deleteIdArg);
        deleteCommand.AddOption(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string projectKey, int keyId, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete deploy key #{keyId}? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteProjectDeployKeyHandler>()
                    .HandleAsync(new DeleteProjectDeployKeyRequest(workspace, projectKey, keyId), CancellationToken.None));
        }, workspaceOption, projectKeyOption, deleteIdArg, yesOption);
        dkCommand.AddCommand(deleteCommand);

        return dkCommand;
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
