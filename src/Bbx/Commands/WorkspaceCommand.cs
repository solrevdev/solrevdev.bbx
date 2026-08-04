using System.CommandLine;
using Bbx.Features.Workspaces.Hooks.CreateWorkspaceHook;
using Bbx.Features.Workspaces.Hooks.DeleteWorkspaceHook;
using Bbx.Features.Workspaces.Hooks.ListWorkspaceHooks;
using Bbx.Features.Workspaces.Hooks.UpdateWorkspaceHook;
using Bbx.Features.Workspaces.Hooks.ViewWorkspaceHook;
using Bbx.Features.Workspaces.ListWorkspaceMembers;
using Bbx.Features.Workspaces.ListWorkspacePermissions;
using Bbx.Features.Workspaces.ListWorkspaces;
using Bbx.Features.Workspaces.Projects.BranchingModel.UpdateProjectBranchingModelSettings;
using Bbx.Features.Workspaces.Projects.BranchingModel.ViewProjectBranchingModel;
using Bbx.Features.Workspaces.Projects.CreateProject;
using Bbx.Features.Workspaces.Projects.DefaultReviewers.AddProjectDefaultReviewer;
using Bbx.Features.Workspaces.Projects.DefaultReviewers.ListProjectDefaultReviewers;
using Bbx.Features.Workspaces.Projects.DefaultReviewers.RemoveProjectDefaultReviewer;
using Bbx.Features.Workspaces.Projects.DeleteProject;
using Bbx.Features.Workspaces.Projects.DeployKeys.AddProjectDeployKey;
using Bbx.Features.Workspaces.Projects.DeployKeys.DeleteProjectDeployKey;
using Bbx.Features.Workspaces.Projects.DeployKeys.ListProjectDeployKeys;
using Bbx.Features.Workspaces.Projects.DeployKeys.ViewProjectDeployKey;
using Bbx.Features.Workspaces.Projects.ListProjects;
using Bbx.Features.Workspaces.Projects.ViewProject;
using Bbx.Features.Workspaces.ViewWorkspace;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class WorkspaceCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("workspace", "Manage Bitbucket workspaces");

        command.Subcommands.Add(CreateListCommand(services));
        command.Subcommands.Add(CreateViewCommand(services));
        command.Subcommands.Add(CreateMembersCommand(services));
        command.Subcommands.Add(CreatePermissionsCommand(services));
        command.Subcommands.Add(CreateHooksCommand(services));
        command.Subcommands.Add(CreateProjectCommand(services));

        return command;
    }

    private static Command CreateProjectCommand(IServiceProvider services)
    {
        // Phase 4 consolidation: `project` is canonical. The flat-flag
        // `projects` (plural) group from Phase 0.5 and the per-verb `project`
        // (singular) group added in Phase 3 are now one subcommand graph
        // covering both the project CRUD (list/view/create/delete) and the
        // per-project sub-APIs (default-reviewers, branching-model,
        // deploy-keys). `projects` (plural) is kept as a soft-deprecated
        // alias so existing scripts that wrote `bbx workspace projects …`
        // continue to work; new docs use the singular.
        var projectCommand = new Command("project",
            "Manage workspace projects (CRUD + per-project sub-APIs)");
        projectCommand.Aliases.Add("projects");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (defaults to configured)" };
        projectCommand.AddRecursiveOption(workspaceOption);

        projectCommand.Subcommands.Add(CreateProjectListCommand(services, workspaceOption));
        projectCommand.Subcommands.Add(CreateProjectViewCommand(services, workspaceOption));
        projectCommand.Subcommands.Add(CreateProjectCreateCommand(services, workspaceOption));
        projectCommand.Subcommands.Add(CreateProjectDeleteCommand(services, workspaceOption));

        var projectKeyOption = new Option<string>("--project-key") { Description = "Project key", Required = true };
        projectCommand.Subcommands.Add(CreateProjectDefaultReviewersCommand(services, workspaceOption, projectKeyOption));
        projectCommand.Subcommands.Add(CreateProjectBranchingModelCommand(services, workspaceOption, projectKeyOption));
        projectCommand.Subcommands.Add(CreateProjectDeployKeysCommand(services, workspaceOption, projectKeyOption));

        return projectCommand;
    }

    private static Command CreateProjectListCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var listCommand = new Command("list", "List workspace projects");
        var limitOption = new Option<int>("--limit", "-l") { Description = "Maximum projects to list", DefaultValueFactory = _ => 25 };
        listCommand.Options.Add(limitOption);
        listCommand.SetHandler((string? workspace, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListProjectsHandler>()
                    .HandleAsync(new ListProjectsRequest(workspace, limit), CommandBinding.CancellationToken)),
            workspaceOption, limitOption);
        return listCommand;
    }

    private static Command CreateProjectViewCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var viewCommand = new Command("view", "View a workspace project");
        var keyArg = new Argument<string>("project-key") { Description = "Project key" };
        viewCommand.Arguments.Add(keyArg);
        viewCommand.SetHandler((string? workspace, string key) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewProjectHandler>()
                    .HandleAsync(new ViewProjectRequest(workspace, key), CommandBinding.CancellationToken)),
            workspaceOption, keyArg);
        return viewCommand;
    }

    private static Command CreateProjectCreateCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var createCommand = new Command("create", "Create a workspace project");
        var keyOption = new Option<string>("--key", "-k") { Description = "Project key", Required = true };
        var nameOption = new Option<string>("--name", "-n") { Description = "Project name", Required = true };
        var descriptionOption = new Option<string?>("--description", "-d") { Description = "Project description" };
        var privateOption = new Option<bool>("--private", "-p") { Description = "Make project private", DefaultValueFactory = _ => true };
        createCommand.Options.Add(keyOption);
        createCommand.Options.Add(nameOption);
        createCommand.Options.Add(descriptionOption);
        createCommand.Options.Add(privateOption);
        createCommand.SetHandler((string? workspace, string key, string name, string? description, bool isPrivate) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateProjectHandler>()
                    .HandleAsync(new CreateProjectRequest(workspace, key, name, description, isPrivate), CommandBinding.CancellationToken)),
            workspaceOption, keyOption, nameOption, descriptionOption, privateOption);
        return createCommand;
    }

    private static Command CreateProjectDeleteCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var deleteCommand = new Command("delete", "Delete a workspace project");
        var keyArg = new Argument<string>("project-key") { Description = "Project key" };
        var yesOption = new Option<bool>("--yes", "-y") { Description = "Skip confirmation", DefaultValueFactory = _ => false };
        deleteCommand.Arguments.Add(keyArg);
        deleteCommand.Options.Add(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string key, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete project '{key}'? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteProjectHandler>()
                    .HandleAsync(new DeleteProjectRequest(workspace, key), CommandBinding.CancellationToken));
        }, workspaceOption, keyArg, yesOption);
        return deleteCommand;
    }

    private static Command CreateProjectDefaultReviewersCommand(IServiceProvider services, Option<string?> workspaceOption, Option<string> projectKeyOption)
    {
        var drCommand = new Command("default-reviewers", "Manage project-level default reviewers");
        // The handlers bind this option, so it has to be registered too. Without
        // that the project key parsed as null and every request went to
        // /workspaces/{ws}/projects//default-reviewers.
        drCommand.AddRecursiveOption(projectKeyOption);

        var listCommand = new Command("list", "List project default reviewers");
        var listLimitOption = new Option<int>("--limit") { Description = "Maximum reviewers to list", DefaultValueFactory = _ => 25 };
        listCommand.Options.Add(listLimitOption);
        listCommand.SetHandler((string? workspace, string projectKey, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListProjectDefaultReviewersHandler>()
                    .HandleAsync(new ListProjectDefaultReviewersRequest(workspace, projectKey, limit), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption, listLimitOption);
        drCommand.Subcommands.Add(listCommand);

        var addCommand = new Command("add", "Add a project default reviewer");
        var addTargetOption = new Option<string>("--target") { Description = "Account ID or UUID of the user", Required = true };
        addCommand.Options.Add(addTargetOption);
        addCommand.SetHandler((string? workspace, string projectKey, string target) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddProjectDefaultReviewerHandler>()
                    .HandleAsync(new AddProjectDefaultReviewerRequest(workspace, projectKey, target), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption, addTargetOption);
        drCommand.Subcommands.Add(addCommand);

        var removeCommand = new Command("remove", "Remove a project default reviewer");
        var removeTargetOption = new Option<string>("--target") { Description = "Account ID or UUID of the user", Required = true };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        removeCommand.Options.Add(removeTargetOption);
        removeCommand.Options.Add(yesOption);
        removeCommand.SetHandler(async (string? workspace, string projectKey, string target, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Remove default reviewer '{target}'? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<RemoveProjectDefaultReviewerHandler>()
                    .HandleAsync(new RemoveProjectDefaultReviewerRequest(workspace, projectKey, target), CommandBinding.CancellationToken));
        }, workspaceOption, projectKeyOption, removeTargetOption, yesOption);
        drCommand.Subcommands.Add(removeCommand);

        return drCommand;
    }

    private static Command CreateProjectBranchingModelCommand(IServiceProvider services, Option<string?> workspaceOption, Option<string> projectKeyOption)
    {
        var bmCommand = new Command("branching-model",
            "Inspect or update the project branching-model defaults");
        bmCommand.AddRecursiveOption(projectKeyOption);

        var viewCommand = new Command("view", "Show the project branching-model defaults");
        viewCommand.SetHandler((string? workspace, string projectKey) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewProjectBranchingModelHandler>()
                    .HandleAsync(new ViewProjectBranchingModelRequest(workspace, projectKey), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption);
        bmCommand.Subcommands.Add(viewCommand);

        var updateCommand = new Command("update",
            "Replace project branching-model settings (PUT raw JSON payload to /branching-model/settings)");
        var settingsJsonOption = new Option<string>("--settings") { Description = "JSON payload", Required = true };
        updateCommand.Options.Add(settingsJsonOption);
        updateCommand.SetHandler((string? workspace, string projectKey, string settingsJson) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateProjectBranchingModelSettingsHandler>()
                    .HandleAsync(new UpdateProjectBranchingModelSettingsRequest(workspace, projectKey, settingsJson), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption, settingsJsonOption);
        bmCommand.Subcommands.Add(updateCommand);

        return bmCommand;
    }

    private static Command CreateProjectDeployKeysCommand(IServiceProvider services, Option<string?> workspaceOption, Option<string> projectKeyOption)
    {
        var dkCommand = new Command("deploy-keys", "Manage project-level deploy keys");
        dkCommand.AddRecursiveOption(projectKeyOption);

        var listCommand = new Command("list", "List project deploy keys");
        var listLimitOption = new Option<int>("--limit") { Description = "Maximum keys to list", DefaultValueFactory = _ => 25 };
        listCommand.Options.Add(listLimitOption);
        listCommand.SetHandler((string? workspace, string projectKey, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListProjectDeployKeysHandler>()
                    .HandleAsync(new ListProjectDeployKeysRequest(workspace, projectKey, limit), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption, listLimitOption);
        dkCommand.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View a project deploy key");
        var viewIdArg = new Argument<int>("key-id") { Description = "Deploy key ID" };
        viewCommand.Arguments.Add(viewIdArg);
        viewCommand.SetHandler((string? workspace, string projectKey, int keyId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewProjectDeployKeyHandler>()
                    .HandleAsync(new ViewProjectDeployKeyRequest(workspace, projectKey, keyId), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption, viewIdArg);
        dkCommand.Subcommands.Add(viewCommand);

        var addCommand = new Command("add", "Add a project deploy key");
        var addKeyOption = new Option<string>("--key") { Description = "Public SSH key body", Required = true };
        var addLabelOption = new Option<string?>("--label") { Description = "Friendly label" };
        addCommand.Options.Add(addKeyOption);
        addCommand.Options.Add(addLabelOption);
        addCommand.SetHandler((string? workspace, string projectKey, string key, string? label) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddProjectDeployKeyHandler>()
                    .HandleAsync(new AddProjectDeployKeyRequest(workspace, projectKey, key, label), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption, addKeyOption, addLabelOption);
        dkCommand.Subcommands.Add(addCommand);

        var deleteCommand = new Command("delete", "Delete a project deploy key");
        var deleteIdArg = new Argument<int>("key-id") { Description = "Deploy key ID" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        deleteCommand.Arguments.Add(deleteIdArg);
        deleteCommand.Options.Add(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string projectKey, int keyId, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete deploy key #{keyId}? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteProjectDeployKeyHandler>()
                    .HandleAsync(new DeleteProjectDeployKeyRequest(workspace, projectKey, keyId), CommandBinding.CancellationToken));
        }, workspaceOption, projectKeyOption, deleteIdArg, yesOption);
        dkCommand.Subcommands.Add(deleteCommand);

        return dkCommand;
    }

    private static Command CreateListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List workspaces the user belongs to");
        var roleOption = new Option<string?>("--role", "-r") { Description = "Filter by role: owner, collaborator, member" };
        var limitOption = new Option<int>("--limit", "-l") { Description = "Maximum number of workspaces to return", DefaultValueFactory = _ => 25 };

        command.Options.Add(roleOption);
        command.Options.Add(limitOption);

        command.SetHandler((string? role, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListWorkspacesHandler>()
                    .HandleAsync(new ListWorkspacesRequest(role, limit), CommandBinding.CancellationToken)),
            roleOption, limitOption);
        return command;
    }

    private static Command CreateViewCommand(IServiceProvider services)
    {
        var command = new Command("view", "View workspace details");
        var workspaceArg = new Argument<string?>("workspace") { Description = "Workspace slug (uses default if not specified)", DefaultValueFactory = _ => null };
        command.Arguments.Add(workspaceArg);

        command.SetHandler((string? workspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewWorkspaceHandler>()
                    .HandleAsync(new ViewWorkspaceRequest(workspace), CommandBinding.CancellationToken)),
            workspaceArg);
        return command;
    }

    private static Command CreateMembersCommand(IServiceProvider services)
    {
        var command = new Command("members", "List workspace members");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };
        var limitOption = new Option<int>("--limit", "-l") { Description = "Maximum number of members to return", DefaultValueFactory = _ => 50 };

        command.Options.Add(workspaceOption);
        command.Options.Add(limitOption);

        command.SetHandler((string? workspace, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListWorkspaceMembersHandler>()
                    .HandleAsync(new ListWorkspaceMembersRequest(workspace, limit), CommandBinding.CancellationToken)),
            workspaceOption, limitOption);
        return command;
    }

    private static Command CreatePermissionsCommand(IServiceProvider services)
    {
        var command = new Command("permissions", "View workspace permissions");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };
        var limitOption = new Option<int>("--limit", "-l") { Description = "Maximum number of permissions to return", DefaultValueFactory = _ => 50 };

        command.Options.Add(workspaceOption);
        command.Options.Add(limitOption);

        command.SetHandler((string? workspace, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListWorkspacePermissionsHandler>()
                    .HandleAsync(new ListWorkspacePermissionsRequest(workspace, limit), CommandBinding.CancellationToken)),
            workspaceOption, limitOption);
        return command;
    }

    private static Command CreateHooksCommand(IServiceProvider services)
    {
        var hooksCommand = new Command("hooks", "Manage workspace webhooks");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };
        hooksCommand.AddRecursiveOption(workspaceOption);

        var listCommand = new Command("list", "List workspace webhooks");
        var limitOption = new Option<int>("--limit") { Description = "Maximum webhooks to list", DefaultValueFactory = _ => 25 };
        listCommand.Options.Add(limitOption);
        listCommand.SetHandler((string? workspace, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListWorkspaceHooksHandler>()
                    .HandleAsync(new ListWorkspaceHooksRequest(workspace, limit), CommandBinding.CancellationToken)),
            workspaceOption, limitOption);
        hooksCommand.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View a workspace webhook");
        var viewUidArg = new Argument<string>("uid") { Description = "Webhook UUID" };
        viewCommand.Arguments.Add(viewUidArg);
        viewCommand.SetHandler((string? workspace, string uid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewWorkspaceHookHandler>()
                    .HandleAsync(new ViewWorkspaceHookRequest(workspace, uid), CommandBinding.CancellationToken)),
            workspaceOption, viewUidArg);
        hooksCommand.Subcommands.Add(viewCommand);

        var createCommand = new Command("create", "Create a workspace webhook");
        var urlOption = new Option<string>("--url") { Description = "Webhook target URL", Required = true };
        var descriptionOption = new Option<string?>("--description") { Description = "Webhook description" };
        var eventsOption = new Option<string[]?>("--events") { Description = "Events to trigger webhook (default: repo:push)" };
        var activeOption = new Option<bool>("--active") { Description = "Whether the webhook is active", DefaultValueFactory = _ => true };
        createCommand.Options.Add(urlOption);
        createCommand.Options.Add(descriptionOption);
        createCommand.Options.Add(eventsOption);
        createCommand.Options.Add(activeOption);
        createCommand.SetHandler((string? workspace, string url, string? description, string[]? events, bool active) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateWorkspaceHookHandler>()
                    .HandleAsync(new CreateWorkspaceHookRequest(workspace, url, description, events, active), CommandBinding.CancellationToken)),
            workspaceOption, urlOption, descriptionOption, eventsOption, activeOption);
        hooksCommand.Subcommands.Add(createCommand);

        var updateCommand = new Command("update", "Update a workspace webhook");
        var updateUidArg = new Argument<string>("uid") { Description = "Webhook UUID" };
        var updateUrlOption = new Option<string?>("--url") { Description = "Webhook target URL" };
        var updateDescriptionOption = new Option<string?>("--description") { Description = "Webhook description" };
        var updateEventsOption = new Option<string[]?>("--events") { Description = "Events to trigger webhook" };
        var updateActiveOption = new Option<bool?>("--active") { Description = "Whether the webhook is active" };
        updateCommand.Arguments.Add(updateUidArg);
        updateCommand.Options.Add(updateUrlOption);
        updateCommand.Options.Add(updateDescriptionOption);
        updateCommand.Options.Add(updateEventsOption);
        updateCommand.Options.Add(updateActiveOption);
        updateCommand.SetHandler((string? workspace, string uid, string? url, string? description, string[]? events, bool? active) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateWorkspaceHookHandler>()
                    .HandleAsync(new UpdateWorkspaceHookRequest(workspace, uid, url, description, events, active), CommandBinding.CancellationToken)),
            workspaceOption, updateUidArg, updateUrlOption, updateDescriptionOption, updateEventsOption, updateActiveOption);
        hooksCommand.Subcommands.Add(updateCommand);

        var deleteCommand = new Command("delete", "Delete a workspace webhook");
        var deleteUidArg = new Argument<string>("uid") { Description = "Webhook UUID" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        deleteCommand.Arguments.Add(deleteUidArg);
        deleteCommand.Options.Add(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string uid, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete webhook '{uid}'? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteWorkspaceHookHandler>()
                    .HandleAsync(new DeleteWorkspaceHookRequest(workspace, uid), CommandBinding.CancellationToken));
        }, workspaceOption, deleteUidArg, yesOption);
        hooksCommand.Subcommands.Add(deleteCommand);

        return hooksCommand;
    }
}
