using System.CommandLine;
using Bbx.Features.Repos.BranchingModel.UpdateBranchingModelSettings;
using Bbx.Features.Repos.BranchingModel.ViewBranchingModel;
using Bbx.Features.Repos.BranchingModel.ViewBranchingModelSettings;
using Bbx.Features.Repos.CloneRepo;
using Bbx.Features.Repos.CreateRepo;
using Bbx.Features.Repos.DefaultReviewers.AddDefaultReviewer;
using Bbx.Features.Repos.DefaultReviewers.EffectiveDefaultReviewers;
using Bbx.Features.Repos.DefaultReviewers.ListDefaultReviewers;
using Bbx.Features.Repos.DefaultReviewers.RemoveDefaultReviewer;
using Bbx.Features.Repos.DeleteRepo;
using Bbx.Features.Repos.DeployKeys.AddRepoDeployKey;
using Bbx.Features.Repos.DeployKeys.DeleteRepoDeployKey;
using Bbx.Features.Repos.DeployKeys.ListRepoDeployKeys;
using Bbx.Features.Repos.DeployKeys.ViewRepoDeployKey;
using Bbx.Features.Repos.ForkRepo;
using Bbx.Features.Repos.Hooks.CreateRepoHook;
using Bbx.Features.Repos.Hooks.DeleteRepoHook;
using Bbx.Features.Repos.Hooks.ListRepoHooks;
using Bbx.Features.Repos.Hooks.UpdateRepoHook;
using Bbx.Features.Repos.Hooks.ViewRepoHook;
using Bbx.Features.Repos.ListForks;
using Bbx.Features.Repos.ListRepos;
using Bbx.Features.Repos.ListWatchers;
using Bbx.Features.Repos.RepoPermissions;
using Bbx.Features.Repos.ViewRepo;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class RepoCommand
{
    public static Command Create(IServiceProvider services)
    {
        var workspaceOption = CommandOptions.CreateWorkspaceOption();
        var command = new Command("repo", "Manage repositories");
        command.AddRecursiveOption(workspaceOption);

        var listCommand = new Command("list", "List repositories");
        var limitOption = new Option<int>("--limit") { Description = "Maximum repositories to list", DefaultValueFactory = _ => 25 };
        var queryOption = new Option<string?>("--query") { Description = "BBQL query filter" };
        listCommand.Options.Add(limitOption);
        listCommand.Options.Add(queryOption);
        listCommand.SetHandler((string? workspace, int limit, string? query) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListReposHandler>()
                    .HandleAsync(new ListReposRequest(workspace, limit, query), CommandBinding.CancellationToken)),
            workspaceOption, limitOption, queryOption);
        command.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View repository details");
        var repoArg = new Argument<string>("repository") { Description = "Repository (workspace/repo or just repo with --workspace)" };
        viewCommand.Arguments.Add(repoArg);
        viewCommand.SetHandler((string? workspace, string repository) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewRepoHandler>()
                    .HandleAsync(new ViewRepoRequest(workspace, repository), CommandBinding.CancellationToken)),
            workspaceOption, repoArg);
        command.Subcommands.Add(viewCommand);

        var createCommand = new Command("create", "Create a new repository");
        var nameArg = new Argument<string>("name") { Description = "Repository name" };
        var privateOption = new Option<bool>("--private") { Description = "Create as private repository" };
        var projectOption = new Option<string?>("--project") { Description = "Project key" };
        var descOption = new Option<string?>("--description") { Description = "Repository description" };
        var forkPolicyOption = new Option<string?>("--fork-policy") { Description = "Fork policy (allow_forks, no_public_forks, no_forks)" };
        createCommand.Arguments.Add(nameArg);
        createCommand.Options.Add(privateOption);
        createCommand.Options.Add(projectOption);
        createCommand.Options.Add(descOption);
        createCommand.Options.Add(forkPolicyOption);
        createCommand.SetHandler((string? workspace, string name, bool isPrivate, string? project, string? description, string? forkPolicy) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateRepoHandler>()
                    .HandleAsync(new CreateRepoRequest(workspace, name, isPrivate, project, description, forkPolicy), CommandBinding.CancellationToken)),
            workspaceOption, nameArg, privateOption, projectOption, descOption, forkPolicyOption);
        command.Subcommands.Add(createCommand);

        var deleteCommand = new Command("delete", "Delete a repository");
        var deleteRepoArg = new Argument<string>("repository") { Description = "Repository to delete" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        deleteCommand.Arguments.Add(deleteRepoArg);
        deleteCommand.Options.Add(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string repository, bool yes) =>
        {
            var (resolvedWs, resolvedRepo) = ParseRepoPath(workspace, repository);
            if (!yes && !CommandRunner.ConfirmOrCancelStderr(
                    $"Delete {resolvedWs}/{resolvedRepo}? This cannot be undone. [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteRepoHandler>()
                    .HandleAsync(new DeleteRepoRequest(workspace, repository), CommandBinding.CancellationToken));
        }, workspaceOption, deleteRepoArg, yesOption);
        command.Subcommands.Add(deleteCommand);

        var forkCommand = new Command("fork", "Fork a repository");
        var forkRepoArg = new Argument<string>("repository") { Description = "Repository to fork" };
        var forkNameOption = new Option<string?>("--name") { Description = "Name for the forked repository" };
        var forkWorkspaceOption = new Option<string?>("--to-workspace") { Description = "Destination workspace for fork" };
        forkCommand.Arguments.Add(forkRepoArg);
        forkCommand.Options.Add(forkNameOption);
        forkCommand.Options.Add(forkWorkspaceOption);
        forkCommand.SetHandler((string? workspace, string repository, string? name, string? toWorkspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ForkRepoHandler>()
                    .HandleAsync(new ForkRepoRequest(workspace, repository, name, toWorkspace), CommandBinding.CancellationToken)),
            workspaceOption, forkRepoArg, forkNameOption, forkWorkspaceOption);
        command.Subcommands.Add(forkCommand);

        var cloneCommand = new Command("clone", "Get clone URL for a repository");
        var cloneRepoArg = new Argument<string>("repository") { Description = "Repository to clone" };
        var sshOption = new Option<bool>("--ssh") { Description = "Get SSH URL instead of HTTPS" };
        cloneCommand.Arguments.Add(cloneRepoArg);
        cloneCommand.Options.Add(sshOption);
        cloneCommand.SetHandler((string? workspace, string repository, bool ssh) =>
            CommandRunner.RunRawAsync(() =>
                services.GetRequiredService<CloneRepoHandler>()
                    .HandleAsync(new CloneRepoRequest(workspace, repository, ssh), CommandBinding.CancellationToken)),
            workspaceOption, cloneRepoArg, sshOption);
        command.Subcommands.Add(cloneCommand);

        var permissionsCommand = new Command("permissions", "View repository permissions");
        var permRepoArg = new Argument<string>("repository") { Description = "Repository" };
        permissionsCommand.Arguments.Add(permRepoArg);
        permissionsCommand.SetHandler((string? workspace, string repository) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<RepoPermissionsHandler>()
                    .HandleAsync(new RepoPermissionsRequest(workspace, repository), CommandBinding.CancellationToken)),
            workspaceOption, permRepoArg);
        command.Subcommands.Add(permissionsCommand);

        command.Subcommands.Add(CreateHooksCommand(services, workspaceOption));

        command.Subcommands.Add(CreateDefaultReviewersCommand(services, workspaceOption));

        command.Subcommands.Add(CreateForksCommand(services, workspaceOption));
        command.Subcommands.Add(CreateWatchersCommand(services, workspaceOption));
        command.Subcommands.Add(CreateBranchingModelCommand(services, workspaceOption));
        command.Subcommands.Add(CreateDeployKeysCommand(services, workspaceOption));

        return command;
    }

    private static Command CreateForksCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var forksCommand = new Command("forks", "List forks of a repository");
        var repoOption = CommandOptions.CreateRepoOption();
        forksCommand.AddRecursiveOption(repoOption);

        var listCommand = new Command("list", "List forks");
        var limitOption = new Option<int>("--limit") { Description = "Maximum forks to list", DefaultValueFactory = _ => 25 };
        listCommand.Options.Add(limitOption);
        listCommand.SetHandler((string? workspace, string? repo, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListForksHandler>()
                    .HandleAsync(new ListForksRequest(workspace, repo, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, limitOption);
        forksCommand.Subcommands.Add(listCommand);

        return forksCommand;
    }

    private static Command CreateWatchersCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var watchersCommand = new Command("watchers", "List repository watchers");
        var repoOption = CommandOptions.CreateRepoOption();
        var limitOption = new Option<int>("--limit") { Description = "Maximum watchers to list", DefaultValueFactory = _ => 25 };
        watchersCommand.Options.Add(repoOption);
        watchersCommand.Options.Add(limitOption);
        watchersCommand.SetHandler((string? workspace, string? repo, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListWatchersHandler>()
                    .HandleAsync(new ListWatchersRequest(workspace, repo, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, limitOption);

        return watchersCommand;
    }

    private static Command CreateBranchingModelCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var bmCommand = new Command("branching-model", "Inspect or update the repository branching model");
        var repoOption = CommandOptions.CreateRepoOption();
        bmCommand.AddRecursiveOption(repoOption);

        var viewCommand = new Command("view", "Show the active branching model (computed)");
        viewCommand.SetHandler((string? workspace, string? repo) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewBranchingModelHandler>()
                    .HandleAsync(new ViewBranchingModelRequest(workspace, repo), CommandBinding.CancellationToken)),
            workspaceOption, repoOption);
        bmCommand.Subcommands.Add(viewCommand);

        var settingsCommand = new Command("settings", "Show the configured branching-model settings");
        settingsCommand.SetHandler((string? workspace, string? repo) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewBranchingModelSettingsHandler>()
                    .HandleAsync(new ViewBranchingModelSettingsRequest(workspace, repo), CommandBinding.CancellationToken)),
            workspaceOption, repoOption);
        bmCommand.Subcommands.Add(settingsCommand);

        var updateCommand = new Command("update",
            "Replace branching-model settings (PUT raw JSON payload)");
        var settingsJsonOption = new Option<string>("--settings")
        { Description = "JSON payload (e.g., '{\"development\":{\"name\":\"main\",\"use_mainbranch\":true}}')", Required = true };
        updateCommand.Options.Add(settingsJsonOption);
        updateCommand.SetHandler((string? workspace, string? repo, string settingsJson) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateBranchingModelSettingsHandler>()
                    .HandleAsync(new UpdateBranchingModelSettingsRequest(workspace, repo, settingsJson), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, settingsJsonOption);
        bmCommand.Subcommands.Add(updateCommand);

        return bmCommand;
    }

    private static Command CreateDeployKeysCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var dkCommand = new Command("deploy-keys", "Manage repository deploy keys");
        var repoOption = CommandOptions.CreateRepoOption();
        dkCommand.AddRecursiveOption(repoOption);

        var listCommand = new Command("list", "List deploy keys");
        var limitOption = new Option<int>("--limit") { Description = "Maximum keys to list", DefaultValueFactory = _ => 25 };
        listCommand.Options.Add(limitOption);
        listCommand.SetHandler((string? workspace, string? repo, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListRepoDeployKeysHandler>()
                    .HandleAsync(new ListRepoDeployKeysRequest(workspace, repo, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, limitOption);
        dkCommand.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View a deploy key");
        var viewIdArg = new Argument<int>("key-id") { Description = "Deploy key ID" };
        viewCommand.Arguments.Add(viewIdArg);
        viewCommand.SetHandler((string? workspace, string? repo, int keyId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewRepoDeployKeyHandler>()
                    .HandleAsync(new ViewRepoDeployKeyRequest(workspace, repo, keyId), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, viewIdArg);
        dkCommand.Subcommands.Add(viewCommand);

        var addCommand = new Command("add", "Add a deploy key");
        var addKeyOption = new Option<string>("--key") { Description = "Public SSH key body", Required = true };
        var addLabelOption = new Option<string?>("--label") { Description = "Friendly label" };
        addCommand.Options.Add(addKeyOption);
        addCommand.Options.Add(addLabelOption);
        addCommand.SetHandler((string? workspace, string? repo, string key, string? label) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddRepoDeployKeyHandler>()
                    .HandleAsync(new AddRepoDeployKeyRequest(workspace, repo, key, label), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, addKeyOption, addLabelOption);
        dkCommand.Subcommands.Add(addCommand);

        var deleteCommand = new Command("delete", "Delete a deploy key");
        var deleteIdArg = new Argument<int>("key-id") { Description = "Deploy key ID" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        deleteCommand.Arguments.Add(deleteIdArg);
        deleteCommand.Options.Add(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string? repo, int keyId, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete deploy key #{keyId}? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteRepoDeployKeyHandler>()
                    .HandleAsync(new DeleteRepoDeployKeyRequest(workspace, repo, keyId), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, deleteIdArg, yesOption);
        dkCommand.Subcommands.Add(deleteCommand);

        return dkCommand;
    }

    private static Command CreateDefaultReviewersCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var drCommand = new Command("default-reviewers", "Manage default reviewers");
        var repoOption = CommandOptions.CreateRepoOption();
        drCommand.AddRecursiveOption(repoOption);

        var listCommand = new Command("list", "List configured default reviewers");
        var listLimitOption = new Option<int>("--limit") { Description = "Maximum reviewers to list", DefaultValueFactory = _ => 25 };
        listCommand.Options.Add(listLimitOption);
        listCommand.SetHandler((string? workspace, string? repo, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListDefaultReviewersHandler>()
                    .HandleAsync(new ListDefaultReviewersRequest(workspace, repo, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, listLimitOption);
        drCommand.Subcommands.Add(listCommand);

        var addCommand = new Command("add", "Add a default reviewer");
        var addTargetOption = new Option<string>("--target") { Description = "Account ID or UUID of the user", Required = true };
        addCommand.Options.Add(addTargetOption);
        addCommand.SetHandler((string? workspace, string? repo, string target) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddDefaultReviewerHandler>()
                    .HandleAsync(new AddDefaultReviewerRequest(workspace, repo, target), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, addTargetOption);
        drCommand.Subcommands.Add(addCommand);

        var removeCommand = new Command("remove", "Remove a default reviewer");
        var removeTargetOption = new Option<string>("--target") { Description = "Account ID or UUID of the user", Required = true };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        removeCommand.Options.Add(removeTargetOption);
        removeCommand.Options.Add(yesOption);
        removeCommand.SetHandler(async (string? workspace, string? repo, string target, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Remove default reviewer '{target}'? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<RemoveDefaultReviewerHandler>()
                    .HandleAsync(new RemoveDefaultReviewerRequest(workspace, repo, target), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, removeTargetOption, yesOption);
        drCommand.Subcommands.Add(removeCommand);

        var effectiveCommand = new Command("effective",
            "List effective default reviewers (includes inherited from project)");
        var effectiveLimitOption = new Option<int>("--limit") { Description = "Maximum reviewers to list", DefaultValueFactory = _ => 25 };
        effectiveCommand.Options.Add(effectiveLimitOption);
        effectiveCommand.SetHandler((string? workspace, string? repo, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<EffectiveDefaultReviewersHandler>()
                    .HandleAsync(new EffectiveDefaultReviewersRequest(workspace, repo, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, effectiveLimitOption);
        drCommand.Subcommands.Add(effectiveCommand);

        return drCommand;
    }

    private static Command CreateHooksCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var hooksCommand = new Command("hooks", "Manage repository webhooks");
        var repoOption = CommandOptions.CreateRepoOption();
        hooksCommand.AddRecursiveOption(repoOption);

        var listCommand = new Command("list", "List repository webhooks");
        var limitOption = new Option<int>("--limit") { Description = "Maximum webhooks to list", DefaultValueFactory = _ => 25 };
        listCommand.Options.Add(limitOption);
        listCommand.SetHandler((string? workspace, string? repo, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListRepoHooksHandler>()
                    .HandleAsync(new ListRepoHooksRequest(workspace, repo, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, limitOption);
        hooksCommand.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View a repository webhook");
        var viewUidArg = new Argument<string>("uid") { Description = "Webhook UUID" };
        viewCommand.Arguments.Add(viewUidArg);
        viewCommand.SetHandler((string? workspace, string? repo, string uid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewRepoHookHandler>()
                    .HandleAsync(new ViewRepoHookRequest(workspace, repo, uid), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, viewUidArg);
        hooksCommand.Subcommands.Add(viewCommand);

        var createCommand = new Command("create", "Create a repository webhook");
        var urlOption = new Option<string>("--url") { Description = "Webhook target URL", Required = true };
        var descriptionOption = new Option<string?>("--description") { Description = "Webhook description" };
        var eventsOption = new Option<string[]?>("--events") { Description = "Events to trigger webhook (default: repo:push)" };
        var activeOption = new Option<bool>("--active") { Description = "Whether the webhook is active", DefaultValueFactory = _ => true };
        createCommand.Options.Add(urlOption);
        createCommand.Options.Add(descriptionOption);
        createCommand.Options.Add(eventsOption);
        createCommand.Options.Add(activeOption);
        createCommand.SetHandler((string? workspace, string? repo, string url, string? description, string[]? events, bool active) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateRepoHookHandler>()
                    .HandleAsync(new CreateRepoHookRequest(workspace, repo, url, description, events, active), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, urlOption, descriptionOption, eventsOption, activeOption);
        hooksCommand.Subcommands.Add(createCommand);

        var updateCommand = new Command("update", "Update a repository webhook");
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
        updateCommand.SetHandler((string? workspace, string? repo, string uid, string? url, string? description, string[]? events, bool? active) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateRepoHookHandler>()
                    .HandleAsync(new UpdateRepoHookRequest(workspace, repo, uid, url, description, events, active), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, updateUidArg, updateUrlOption, updateDescriptionOption, updateEventsOption, updateActiveOption);
        hooksCommand.Subcommands.Add(updateCommand);

        var deleteCommand = new Command("delete", "Delete a repository webhook");
        var deleteUidArg = new Argument<string>("uid") { Description = "Webhook UUID" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        deleteCommand.Arguments.Add(deleteUidArg);
        deleteCommand.Options.Add(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string? repo, string uid, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete webhook '{uid}'? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteRepoHookHandler>()
                    .HandleAsync(new DeleteRepoHookRequest(workspace, repo, uid), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, deleteUidArg, yesOption);
        hooksCommand.Subcommands.Add(deleteCommand);

        return hooksCommand;
    }

    private static (string ws, string repo) ParseRepoPath(string? workspace, string path)
    {
        if (path.Contains('/'))
        {
            var parts = path.Split('/', 2);
            return (parts[0], parts[1]);
        }
        return (workspace ?? "<workspace>", path);
    }
}
