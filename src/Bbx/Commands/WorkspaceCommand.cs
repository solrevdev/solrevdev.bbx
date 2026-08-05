using System.CommandLine;
using Bbx.Features.Workspaces.ListWorkspacePullRequests;
using Bbx.Features.Workspaces.ListWorkspaceRepositoryPermissions;
using Bbx.Features.Workspaces.Pipelines.AddWorkspaceVariable;
using Bbx.Features.Workspaces.Pipelines.DeleteWorkspaceVariable;
using Bbx.Features.Workspaces.Pipelines.ListWorkspaceVariables;
using Bbx.Features.Workspaces.Pipelines.UpdateWorkspaceVariable;
using Bbx.Features.Workspaces.Pipelines.ViewWorkspaceOidcConfig;
using Bbx.Features.Workspaces.Pipelines.ViewWorkspaceOidcKeys;
using Bbx.Features.Workspaces.Pipelines.ViewWorkspaceVariable;
using Bbx.Features.Workspaces.Projects.BranchingModel.ViewProjectBranchingModelSettings;
using Bbx.Features.Workspaces.Projects.DefaultReviewers.ViewProjectDefaultReviewer;
using Bbx.Features.Workspaces.Projects.UpdateProject;
using Bbx.Features.Workspaces.ViewWorkspaceGpgKey;
using Bbx.Features.Workspaces.ViewWorkspaceMember;
using Bbx.Features.Workspaces.Projects.Access.ListProjectGroupPermissions;
using Bbx.Features.Workspaces.Projects.Access.ListProjectUserPermissions;
using Bbx.Features.Workspaces.Projects.Access.RemoveProjectGroupPermission;
using Bbx.Features.Workspaces.Projects.Access.RemoveProjectUserPermission;
using Bbx.Features.Workspaces.Projects.Access.SetProjectGroupPermission;
using Bbx.Features.Workspaces.Projects.Access.SetProjectUserPermission;
using Bbx.Features.Workspaces.Projects.Access.ViewProjectGroupPermission;
using Bbx.Features.Workspaces.Projects.Access.ViewProjectUserPermission;
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
        command.Subcommands.Add(CreateMemberCommand(services));
        command.Subcommands.Add(CreateRepoPermissionsCommand(services));
        command.Subcommands.Add(CreateGpgKeyCommand(services));
        command.Subcommands.Add(CreateWorkspacePullRequestsCommand(services));
        command.Subcommands.Add(CreateWorkspacePipelinesCommand(services));
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
        projectCommand.Subcommands.Add(CreateProjectUpdateCommand(services, workspaceOption));

        var projectKeyOption = new Option<string>("--project-key") { Description = "Project key", Required = true };
        projectCommand.Subcommands.Add(CreateProjectDefaultReviewersCommand(services, workspaceOption, projectKeyOption));
        projectCommand.Subcommands.Add(CreateProjectBranchingModelCommand(services, workspaceOption, projectKeyOption));
        projectCommand.Subcommands.Add(CreateProjectDeployKeysCommand(services, workspaceOption, projectKeyOption));
        projectCommand.Subcommands.Add(CreateProjectAccessCommand(services, workspaceOption, projectKeyOption));

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

        var viewCommand = new Command("view", "Check whether a user is a project default reviewer");
        var viewTargetOption = new Option<string>("--target") { Description = "Account ID or UUID of the user", Required = true };
        viewCommand.Options.Add(viewTargetOption);
        viewCommand.SetHandler((string? workspace, string projectKey, string target) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewProjectDefaultReviewerHandler>()
                    .HandleAsync(new ViewProjectDefaultReviewerRequest(workspace, projectKey, target), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption, viewTargetOption);
        drCommand.Subcommands.Add(viewCommand);

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

        var settingsCommand = new Command("settings", "Show the configured project branching-model settings");
        settingsCommand.SetHandler((string? workspace, string projectKey) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewProjectBranchingModelSettingsHandler>()
                    .HandleAsync(new ViewProjectBranchingModelSettingsRequest(workspace, projectKey), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption);
        bmCommand.Subcommands.Add(settingsCommand);

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

    /// <summary>
    /// The explicit permission grants on a project. Repository grants sit under
    /// `bbx repo access`; these are the ones every repository in the project
    /// inherits.
    /// </summary>
    private static Command CreateProjectAccessCommand(
        IServiceProvider services, Option<string?> workspaceOption, Option<string> projectKeyOption)
    {
        var accessCommand = new Command("access", "Read and set explicit project permissions");
        // The handlers bind this option, so it has to be registered here too,
        // the same way the default-reviewers group does it.
        accessCommand.AddRecursiveOption(projectKeyOption);

        var groupsCommand = new Command("groups", "Explicit group permissions on the project");

        var groupsListCommand = new Command("list", "List explicit group permissions");
        var groupsLimitOption = new Option<int>("--limit") { Description = "Maximum grants to list", DefaultValueFactory = _ => 50 };
        groupsListCommand.Options.Add(groupsLimitOption);
        groupsListCommand.SetHandler((string? workspace, string projectKey, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListProjectGroupPermissionsHandler>()
                    .HandleAsync(new ListProjectGroupPermissionsRequest(workspace, projectKey, limit), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption, groupsLimitOption);
        groupsCommand.Subcommands.Add(groupsListCommand);

        var groupsViewCommand = new Command("view", "View one group's explicit permission");
        var groupsViewSlugArg = new Argument<string>("group-slug") { Description = "Group slug" };
        groupsViewCommand.Arguments.Add(groupsViewSlugArg);
        groupsViewCommand.SetHandler((string? workspace, string projectKey, string slug) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewProjectGroupPermissionHandler>()
                    .HandleAsync(new ViewProjectGroupPermissionRequest(workspace, projectKey, slug), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption, groupsViewSlugArg);
        groupsCommand.Subcommands.Add(groupsViewCommand);

        var groupsSetCommand = new Command("set", "Grant a group a permission on the project");
        var groupsSetSlugArg = new Argument<string>("group-slug") { Description = "Group slug" };
        var groupsSetPermissionOption = new Option<string>("--permission")
        { Description = "Permission to grant (read, write, create-repo, admin, none)", Required = true };
        groupsSetCommand.Arguments.Add(groupsSetSlugArg);
        groupsSetCommand.Options.Add(groupsSetPermissionOption);
        groupsSetCommand.SetHandler((string? workspace, string projectKey, string slug, string permission) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<SetProjectGroupPermissionHandler>()
                    .HandleAsync(new SetProjectGroupPermissionRequest(workspace, projectKey, slug, permission), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption, groupsSetSlugArg, groupsSetPermissionOption);
        groupsCommand.Subcommands.Add(groupsSetCommand);

        var groupsRemoveCommand = new Command("remove", "Remove a group's explicit permission");
        var groupsRemoveSlugArg = new Argument<string>("group-slug") { Description = "Group slug" };
        var groupsYesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        groupsRemoveCommand.Arguments.Add(groupsRemoveSlugArg);
        groupsRemoveCommand.Options.Add(groupsYesOption);
        groupsRemoveCommand.SetHandler(async (string? workspace, string projectKey, string slug, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr(
                    $"Remove the explicit permission for group '{slug}' on {projectKey}? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<RemoveProjectGroupPermissionHandler>()
                    .HandleAsync(new RemoveProjectGroupPermissionRequest(workspace, projectKey, slug), CommandBinding.CancellationToken));
        }, workspaceOption, projectKeyOption, groupsRemoveSlugArg, groupsYesOption);
        groupsCommand.Subcommands.Add(groupsRemoveCommand);

        accessCommand.Subcommands.Add(groupsCommand);

        var usersCommand = new Command("users", "Explicit user permissions on the project");

        var usersListCommand = new Command("list", "List explicit user permissions");
        var usersLimitOption = new Option<int>("--limit") { Description = "Maximum grants to list", DefaultValueFactory = _ => 50 };
        usersListCommand.Options.Add(usersLimitOption);
        usersListCommand.SetHandler((string? workspace, string projectKey, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListProjectUserPermissionsHandler>()
                    .HandleAsync(new ListProjectUserPermissionsRequest(workspace, projectKey, limit), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption, usersLimitOption);
        usersCommand.Subcommands.Add(usersListCommand);

        var usersViewCommand = new Command("view", "View one user's explicit permission");
        var usersViewIdArg = new Argument<string>("account-id") { Description = "Account ID or UUID" };
        usersViewCommand.Arguments.Add(usersViewIdArg);
        usersViewCommand.SetHandler((string? workspace, string projectKey, string accountId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewProjectUserPermissionHandler>()
                    .HandleAsync(new ViewProjectUserPermissionRequest(workspace, projectKey, accountId), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption, usersViewIdArg);
        usersCommand.Subcommands.Add(usersViewCommand);

        var usersSetCommand = new Command("set", "Grant a user a permission on the project");
        var usersSetIdArg = new Argument<string>("account-id") { Description = "Account ID or UUID" };
        var usersSetPermissionOption = new Option<string>("--permission")
        { Description = "Permission to grant (read, write, create-repo, admin, none)", Required = true };
        usersSetCommand.Arguments.Add(usersSetIdArg);
        usersSetCommand.Options.Add(usersSetPermissionOption);
        usersSetCommand.SetHandler((string? workspace, string projectKey, string accountId, string permission) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<SetProjectUserPermissionHandler>()
                    .HandleAsync(new SetProjectUserPermissionRequest(workspace, projectKey, accountId, permission), CommandBinding.CancellationToken)),
            workspaceOption, projectKeyOption, usersSetIdArg, usersSetPermissionOption);
        usersCommand.Subcommands.Add(usersSetCommand);

        var usersRemoveCommand = new Command("remove", "Remove a user's explicit permission");
        var usersRemoveIdArg = new Argument<string>("account-id") { Description = "Account ID or UUID" };
        var usersYesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        usersRemoveCommand.Arguments.Add(usersRemoveIdArg);
        usersRemoveCommand.Options.Add(usersYesOption);
        usersRemoveCommand.SetHandler(async (string? workspace, string projectKey, string accountId, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr(
                    $"Remove the explicit permission for '{accountId}' on {projectKey}? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<RemoveProjectUserPermissionHandler>()
                    .HandleAsync(new RemoveProjectUserPermissionRequest(workspace, projectKey, accountId), CommandBinding.CancellationToken));
        }, workspaceOption, projectKeyOption, usersRemoveIdArg, usersYesOption);
        usersCommand.Subcommands.Add(usersRemoveCommand);

        accessCommand.Subcommands.Add(usersCommand);

        return accessCommand;
    }


    private static Command CreateProjectUpdateCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var command = new Command("update", "Update a workspace project");
        var keyArg = new Argument<string>("project-key") { Description = "Project key" };
        var nameOption = new Option<string?>("--name") { Description = "New project name" };
        var descOption = new Option<string?>("--description") { Description = "New description. Pass an empty string to clear it." };
        var privateOption = new Option<bool>("--private") { Description = "Make the project private" };
        var publicOption = new Option<bool>("--public") { Description = "Make the project public" };
        var newKeyOption = new Option<string?>("--new-key")
        { Description = "Rename the project key. Every repository in the project moves with it." };

        command.Arguments.Add(keyArg);
        command.Options.Add(nameOption);
        command.Options.Add(descOption);
        command.Options.Add(privateOption);
        command.Options.Add(publicOption);
        command.Options.Add(newKeyOption);

        command.SetHandler((string? workspace, string projectKey, string? name, string? description, bool isPrivate, bool isPublic, string? newKey) =>
            CommandRunner.RunJsonAsync(() =>
            {
                if (isPrivate && isPublic)
                    throw new BbxUserException("Error: --private and --public are mutually exclusive.");
                bool? visibility = isPrivate ? true : isPublic ? false : null;
                return services.GetRequiredService<UpdateProjectHandler>()
                    .HandleAsync(new UpdateProjectRequest(workspace, projectKey, name, description, visibility, newKey), CommandBinding.CancellationToken);
            }),
            workspaceOption, keyArg, nameOption, descOption, privateOption, publicOption, newKeyOption);
        return command;
    }

    private static Command CreateMemberCommand(IServiceProvider services)
    {
        var command = new Command("member", "View one workspace member");
        var memberArg = new Argument<string>("member")
        { Description = "Account UUID or account ID. Usernames are no longer accepted by Bitbucket." };
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };
        command.Arguments.Add(memberArg);
        command.Options.Add(workspaceOption);
        command.SetHandler((string member, string? workspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewWorkspaceMemberHandler>()
                    .HandleAsync(new ViewWorkspaceMemberRequest(workspace, member), CommandBinding.CancellationToken)),
            memberArg, workspaceOption);
        return command;
    }

    private static Command CreateRepoPermissionsCommand(IServiceProvider services)
    {
        var command = new Command("repo-permissions",
            "List repository permissions across the workspace, or for one repository with --repo");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Narrow to one repository slug" };
        var limitOption = new Option<int>("--limit", "-l") { Description = "Maximum grants to list", DefaultValueFactory = _ => 50 };
        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Options.Add(limitOption);
        command.SetHandler((string? workspace, string? repo, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListWorkspaceRepositoryPermissionsHandler>()
                    .HandleAsync(new ListWorkspaceRepositoryPermissionsRequest(workspace, repo, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, limitOption);
        return command;
    }

    private static Command CreateGpgKeyCommand(IServiceProvider services)
    {
        var command = new Command("gpg-key",
            "Show the GPG key Bitbucket signs the workspace's web-edit commits with");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };
        command.Options.Add(workspaceOption);
        command.SetHandler((string? workspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewWorkspaceGpgKeyHandler>()
                    .HandleAsync(new ViewWorkspaceGpgKeyRequest(workspace), CommandBinding.CancellationToken)),
            workspaceOption);
        return command;
    }

    private static Command CreateWorkspacePullRequestsCommand(IServiceProvider services)
    {
        var command = new Command("pullrequests",
            "List a user's pull requests across the whole workspace, not just one repository");
        var userArg = new Argument<string>("user")
        { Description = "Account UUID or account ID whose pull requests to list" };
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };
        var stateOption = new Option<string?>("--state") { Description = "Filter by state (OPEN, MERGED, DECLINED, SUPERSEDED)" };
        var limitOption = new Option<int>("--limit", "-l") { Description = "Maximum pull requests to list", DefaultValueFactory = _ => 25 };
        command.Arguments.Add(userArg);
        command.Options.Add(workspaceOption);
        command.Options.Add(stateOption);
        command.Options.Add(limitOption);
        command.SetHandler((string user, string? workspace, string? state, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListWorkspacePullRequestsHandler>()
                    .HandleAsync(new ListWorkspacePullRequestsRequest(workspace, user, state, limit), CommandBinding.CancellationToken)),
            userArg, workspaceOption, stateOption, limitOption);
        return command;
    }

    /// <summary>
    /// Pipeline settings that live on the workspace rather than a repository.
    /// The repository-scoped equivalents are under `bbx pipeline`.
    /// </summary>
    private static Command CreateWorkspacePipelinesCommand(IServiceProvider services)
    {
        var pipelinesCommand = new Command("pipelines", "Workspace-wide pipeline variables and OIDC discovery");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };
        pipelinesCommand.AddRecursiveOption(workspaceOption);

        var variablesCommand = new Command("variables", "Variables every repository in the workspace inherits");

        var listCommand = new Command("list", "List workspace pipeline variables");
        var listLimitOption = new Option<int>("--limit") { Description = "Maximum variables to list", DefaultValueFactory = _ => 50 };
        listCommand.Options.Add(listLimitOption);
        listCommand.SetHandler((string? workspace, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListWorkspaceVariablesHandler>()
                    .HandleAsync(new ListWorkspaceVariablesRequest(workspace, limit), CommandBinding.CancellationToken)),
            workspaceOption, listLimitOption);
        variablesCommand.Subcommands.Add(listCommand);

        var addCommand = new Command("add", "Add a workspace pipeline variable");
        var addKeyOption = new Option<string>("--key") { Description = "Variable name", Required = true };
        var addValueOption = new Option<string>("--value") { Description = "Variable value", Required = true };
        var addSecuredOption = new Option<bool>("--secured") { Description = "Hide the value from the API and the UI" };
        addCommand.Options.Add(addKeyOption);
        addCommand.Options.Add(addValueOption);
        addCommand.Options.Add(addSecuredOption);
        addCommand.SetHandler((string? workspace, string key, string value, bool secured) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddWorkspaceVariableHandler>()
                    .HandleAsync(new AddWorkspaceVariableRequest(workspace, key, value, secured), CommandBinding.CancellationToken)),
            workspaceOption, addKeyOption, addValueOption, addSecuredOption);
        variablesCommand.Subcommands.Add(addCommand);

        var viewCommand = new Command("view", "View one workspace pipeline variable");
        var viewUuidArg = new Argument<string>("variable-uuid") { Description = "Variable UUID" };
        viewCommand.Arguments.Add(viewUuidArg);
        viewCommand.SetHandler((string? workspace, string variableUuid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewWorkspaceVariableHandler>()
                    .HandleAsync(new ViewWorkspaceVariableRequest(workspace, variableUuid), CommandBinding.CancellationToken)),
            workspaceOption, viewUuidArg);
        variablesCommand.Subcommands.Add(viewCommand);

        var updateCommand = new Command("update", "Change a workspace pipeline variable");
        var updateUuidArg = new Argument<string>("variable-uuid") { Description = "Variable UUID" };
        var updateKeyOption = new Option<string?>("--key") { Description = "New variable name" };
        var updateValueOption = new Option<string?>("--value") { Description = "New value" };
        var updateSecuredOption = new Option<bool>("--secured") { Description = "Hide the value" };
        var updateUnsecuredOption = new Option<bool>("--unsecured") { Description = "Show the value again" };
        updateCommand.Arguments.Add(updateUuidArg);
        updateCommand.Options.Add(updateKeyOption);
        updateCommand.Options.Add(updateValueOption);
        updateCommand.Options.Add(updateSecuredOption);
        updateCommand.Options.Add(updateUnsecuredOption);
        updateCommand.SetHandler((string? workspace, string variableUuid, string? key, string? value, bool secured, bool unsecured) =>
            CommandRunner.RunJsonAsync(() =>
            {
                if (secured && unsecured)
                    throw new BbxUserException("Error: --secured and --unsecured are mutually exclusive.");
                bool? isSecured = secured ? true : unsecured ? false : null;
                return services.GetRequiredService<UpdateWorkspaceVariableHandler>()
                    .HandleAsync(new UpdateWorkspaceVariableRequest(workspace, variableUuid, key, value, isSecured), CommandBinding.CancellationToken);
            }),
            workspaceOption, updateUuidArg, updateKeyOption, updateValueOption, updateSecuredOption, updateUnsecuredOption);
        variablesCommand.Subcommands.Add(updateCommand);

        var deleteCommand = new Command("delete", "Delete a workspace pipeline variable");
        var deleteUuidArg = new Argument<string>("variable-uuid") { Description = "Variable UUID" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        deleteCommand.Arguments.Add(deleteUuidArg);
        deleteCommand.Options.Add(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string variableUuid, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr(
                    $"Delete workspace variable {variableUuid}? Every repository inherits it. [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteWorkspaceVariableHandler>()
                    .HandleAsync(new DeleteWorkspaceVariableRequest(workspace, variableUuid), CommandBinding.CancellationToken));
        }, workspaceOption, deleteUuidArg, yesOption);
        variablesCommand.Subcommands.Add(deleteCommand);

        pipelinesCommand.Subcommands.Add(variablesCommand);

        var oidcCommand = new Command("oidc", "Workspace-level OIDC discovery and JWKS");

        var oidcConfigCommand = new Command("config", "Show the workspace OpenID provider configuration");
        oidcConfigCommand.SetHandler((string? workspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewWorkspaceOidcConfigHandler>()
                    .HandleAsync(new ViewWorkspaceOidcConfigRequest(workspace), CommandBinding.CancellationToken)),
            workspaceOption);
        oidcCommand.Subcommands.Add(oidcConfigCommand);

        var oidcKeysCommand = new Command("keys", "Show the workspace JWKS used to verify pipeline OIDC tokens");
        oidcKeysCommand.SetHandler((string? workspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewWorkspaceOidcKeysHandler>()
                    .HandleAsync(new ViewWorkspaceOidcKeysRequest(workspace), CommandBinding.CancellationToken)),
            workspaceOption);
        oidcCommand.Subcommands.Add(oidcKeysCommand);

        pipelinesCommand.Subcommands.Add(oidcCommand);

        return pipelinesCommand;
    }

}
