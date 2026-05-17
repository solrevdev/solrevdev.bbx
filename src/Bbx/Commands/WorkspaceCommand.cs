using System.CommandLine;
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
using Bbx.Features.Workspaces.Hooks.CreateWorkspaceHook;
using Bbx.Features.Workspaces.Hooks.DeleteWorkspaceHook;
using Bbx.Features.Workspaces.Hooks.ListWorkspaceHooks;
using Bbx.Features.Workspaces.Hooks.UpdateWorkspaceHook;
using Bbx.Features.Workspaces.Hooks.ViewWorkspaceHook;
using Bbx.Features.Workspaces.ViewWorkspace;
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
        command.AddCommand(CreatePermissionsCommand(services));
        command.AddCommand(CreateHooksCommand(services));
        command.AddCommand(CreateProjectCommand(services));

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
        projectCommand.AddAlias("projects");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (defaults to configured)");
        projectCommand.AddGlobalOption(workspaceOption);

        projectCommand.AddCommand(CreateProjectListCommand(services, workspaceOption));
        projectCommand.AddCommand(CreateProjectViewCommand(services, workspaceOption));
        projectCommand.AddCommand(CreateProjectCreateCommand(services, workspaceOption));
        projectCommand.AddCommand(CreateProjectDeleteCommand(services, workspaceOption));

        var projectKeyOption = new Option<string>("--project-key", "Project key") { IsRequired = true };
        projectCommand.AddCommand(CreateProjectDefaultReviewersCommand(services, workspaceOption, projectKeyOption));
        projectCommand.AddCommand(CreateProjectBranchingModelCommand(services, workspaceOption, projectKeyOption));
        projectCommand.AddCommand(CreateProjectDeployKeysCommand(services, workspaceOption, projectKeyOption));

        return projectCommand;
    }

    private static Command CreateProjectListCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var listCommand = new Command("list", "List workspace projects");
        var limitOption = new Option<int>(["--limit", "-l"], () => 25, "Maximum projects to list");
        listCommand.AddOption(limitOption);
        listCommand.SetHandler((string? workspace, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListProjectsHandler>()
                    .HandleAsync(new ListProjectsRequest(workspace, limit), CancellationToken.None)),
            workspaceOption, limitOption);
        return listCommand;
    }

    private static Command CreateProjectViewCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var viewCommand = new Command("view", "View a workspace project");
        var keyArg = new Argument<string>("project-key", "Project key");
        viewCommand.AddArgument(keyArg);
        viewCommand.SetHandler((string? workspace, string key) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewProjectHandler>()
                    .HandleAsync(new ViewProjectRequest(workspace, key), CancellationToken.None)),
            workspaceOption, keyArg);
        return viewCommand;
    }

    private static Command CreateProjectCreateCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var createCommand = new Command("create", "Create a workspace project");
        var keyOption = new Option<string>(["--key", "-k"], "Project key") { IsRequired = true };
        var nameOption = new Option<string>(["--name", "-n"], "Project name") { IsRequired = true };
        var descriptionOption = new Option<string?>(["--description", "-d"], "Project description");
        var privateOption = new Option<bool>(["--private", "-p"], () => true, "Make project private");
        createCommand.AddOption(keyOption);
        createCommand.AddOption(nameOption);
        createCommand.AddOption(descriptionOption);
        createCommand.AddOption(privateOption);
        createCommand.SetHandler((string? workspace, string key, string name, string? description, bool isPrivate) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateProjectHandler>()
                    .HandleAsync(new CreateProjectRequest(workspace, key, name, description, isPrivate), CancellationToken.None)),
            workspaceOption, keyOption, nameOption, descriptionOption, privateOption);
        return createCommand;
    }

    private static Command CreateProjectDeleteCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var deleteCommand = new Command("delete", "Delete a workspace project");
        var keyArg = new Argument<string>("project-key", "Project key");
        var yesOption = new Option<bool>(["--yes", "-y"], () => false, "Skip confirmation");
        deleteCommand.AddArgument(keyArg);
        deleteCommand.AddOption(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string key, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete project '{key}'? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteProjectHandler>()
                    .HandleAsync(new DeleteProjectRequest(workspace, key), CancellationToken.None));
        }, workspaceOption, keyArg, yesOption);
        return deleteCommand;
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
        var hooksCommand = new Command("hooks", "Manage workspace webhooks");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (uses default if not specified)");
        hooksCommand.AddGlobalOption(workspaceOption);

        var listCommand = new Command("list", "List workspace webhooks");
        var limitOption = new Option<int>("--limit", () => 25, "Maximum webhooks to list");
        listCommand.AddOption(limitOption);
        listCommand.SetHandler((string? workspace, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListWorkspaceHooksHandler>()
                    .HandleAsync(new ListWorkspaceHooksRequest(workspace, limit), CancellationToken.None)),
            workspaceOption, limitOption);
        hooksCommand.AddCommand(listCommand);

        var viewCommand = new Command("view", "View a workspace webhook");
        var viewUidArg = new Argument<string>("uid", "Webhook UUID");
        viewCommand.AddArgument(viewUidArg);
        viewCommand.SetHandler((string? workspace, string uid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewWorkspaceHookHandler>()
                    .HandleAsync(new ViewWorkspaceHookRequest(workspace, uid), CancellationToken.None)),
            workspaceOption, viewUidArg);
        hooksCommand.AddCommand(viewCommand);

        var createCommand = new Command("create", "Create a workspace webhook");
        var urlOption = new Option<string>("--url", "Webhook target URL") { IsRequired = true };
        var descriptionOption = new Option<string?>("--description", "Webhook description");
        var eventsOption = new Option<string[]?>("--events", "Events to trigger webhook (default: repo:push)");
        var activeOption = new Option<bool>("--active", () => true, "Whether the webhook is active");
        createCommand.AddOption(urlOption);
        createCommand.AddOption(descriptionOption);
        createCommand.AddOption(eventsOption);
        createCommand.AddOption(activeOption);
        createCommand.SetHandler((string? workspace, string url, string? description, string[]? events, bool active) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateWorkspaceHookHandler>()
                    .HandleAsync(new CreateWorkspaceHookRequest(workspace, url, description, events, active), CancellationToken.None)),
            workspaceOption, urlOption, descriptionOption, eventsOption, activeOption);
        hooksCommand.AddCommand(createCommand);

        var updateCommand = new Command("update", "Update a workspace webhook");
        var updateUidArg = new Argument<string>("uid", "Webhook UUID");
        var updateUrlOption = new Option<string?>("--url", "Webhook target URL");
        var updateDescriptionOption = new Option<string?>("--description", "Webhook description");
        var updateEventsOption = new Option<string[]?>("--events", "Events to trigger webhook");
        var updateActiveOption = new Option<bool?>("--active", "Whether the webhook is active");
        updateCommand.AddArgument(updateUidArg);
        updateCommand.AddOption(updateUrlOption);
        updateCommand.AddOption(updateDescriptionOption);
        updateCommand.AddOption(updateEventsOption);
        updateCommand.AddOption(updateActiveOption);
        updateCommand.SetHandler((string? workspace, string uid, string? url, string? description, string[]? events, bool? active) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateWorkspaceHookHandler>()
                    .HandleAsync(new UpdateWorkspaceHookRequest(workspace, uid, url, description, events, active), CancellationToken.None)),
            workspaceOption, updateUidArg, updateUrlOption, updateDescriptionOption, updateEventsOption, updateActiveOption);
        hooksCommand.AddCommand(updateCommand);

        var deleteCommand = new Command("delete", "Delete a workspace webhook");
        var deleteUidArg = new Argument<string>("uid", "Webhook UUID");
        var yesOption = new Option<bool>("--yes", "Skip confirmation");
        deleteCommand.AddArgument(deleteUidArg);
        deleteCommand.AddOption(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string uid, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete webhook '{uid}'? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteWorkspaceHookHandler>()
                    .HandleAsync(new DeleteWorkspaceHookRequest(workspace, uid), CancellationToken.None));
        }, workspaceOption, deleteUidArg, yesOption);
        hooksCommand.AddCommand(deleteCommand);

        return hooksCommand;
    }
}
