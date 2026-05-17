using System.CommandLine;
using Bbx.Features.Repos.CloneRepo;
using Bbx.Features.Repos.CreateRepo;
using Bbx.Features.Repos.DeleteRepo;
using Bbx.Features.Repos.ForkRepo;
using Bbx.Features.Repos.Hooks.CreateRepoHook;
using Bbx.Features.Repos.Hooks.DeleteRepoHook;
using Bbx.Features.Repos.Hooks.ListRepoHooks;
using Bbx.Features.Repos.Hooks.UpdateRepoHook;
using Bbx.Features.Repos.Hooks.ViewRepoHook;
using Bbx.Features.Repos.ListRepos;
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
        command.AddGlobalOption(workspaceOption);

        var listCommand = new Command("list", "List repositories");
        var limitOption = new Option<int>("--limit", () => 25, "Maximum repositories to list");
        var queryOption = new Option<string?>("--query", "BBQL query filter");
        listCommand.AddOption(limitOption);
        listCommand.AddOption(queryOption);
        listCommand.SetHandler((string? workspace, int limit, string? query) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListReposHandler>()
                    .HandleAsync(new ListReposRequest(workspace, limit, query), CancellationToken.None)),
            workspaceOption, limitOption, queryOption);
        command.AddCommand(listCommand);

        var viewCommand = new Command("view", "View repository details");
        var repoArg = new Argument<string>("repository", "Repository (workspace/repo or just repo with --workspace)");
        viewCommand.AddArgument(repoArg);
        viewCommand.SetHandler((string? workspace, string repository) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewRepoHandler>()
                    .HandleAsync(new ViewRepoRequest(workspace, repository), CancellationToken.None)),
            workspaceOption, repoArg);
        command.AddCommand(viewCommand);

        var createCommand = new Command("create", "Create a new repository");
        var nameArg = new Argument<string>("name", "Repository name");
        var privateOption = new Option<bool>("--private", "Create as private repository");
        var projectOption = new Option<string?>("--project", "Project key");
        var descOption = new Option<string?>("--description", "Repository description");
        var forkPolicyOption = new Option<string?>("--fork-policy", "Fork policy (allow_forks, no_public_forks, no_forks)");
        createCommand.AddArgument(nameArg);
        createCommand.AddOption(privateOption);
        createCommand.AddOption(projectOption);
        createCommand.AddOption(descOption);
        createCommand.AddOption(forkPolicyOption);
        createCommand.SetHandler((string? workspace, string name, bool isPrivate, string? project, string? description, string? forkPolicy) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateRepoHandler>()
                    .HandleAsync(new CreateRepoRequest(workspace, name, isPrivate, project, description, forkPolicy), CancellationToken.None)),
            workspaceOption, nameArg, privateOption, projectOption, descOption, forkPolicyOption);
        command.AddCommand(createCommand);

        var deleteCommand = new Command("delete", "Delete a repository");
        var deleteRepoArg = new Argument<string>("repository", "Repository to delete");
        var yesOption = new Option<bool>("--yes", "Skip confirmation");
        deleteCommand.AddArgument(deleteRepoArg);
        deleteCommand.AddOption(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string repository, bool yes) =>
        {
            var (resolvedWs, resolvedRepo) = ParseRepoPath(workspace, repository);
            if (!yes)
            {
                Console.Write($"Delete {resolvedWs}/{resolvedRepo}? This cannot be undone. (y/N): ");
                if (Console.ReadLine()?.Trim().ToLower() != "y")
                {
                    Console.WriteLine("Cancelled.");
                    return;
                }
            }
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteRepoHandler>()
                    .HandleAsync(new DeleteRepoRequest(workspace, repository), CancellationToken.None));
        }, workspaceOption, deleteRepoArg, yesOption);
        command.AddCommand(deleteCommand);

        var forkCommand = new Command("fork", "Fork a repository");
        var forkRepoArg = new Argument<string>("repository", "Repository to fork");
        var forkNameOption = new Option<string?>("--name", "Name for the forked repository");
        var forkWorkspaceOption = new Option<string?>("--to-workspace", "Destination workspace for fork");
        forkCommand.AddArgument(forkRepoArg);
        forkCommand.AddOption(forkNameOption);
        forkCommand.AddOption(forkWorkspaceOption);
        forkCommand.SetHandler((string? workspace, string repository, string? name, string? toWorkspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ForkRepoHandler>()
                    .HandleAsync(new ForkRepoRequest(workspace, repository, name, toWorkspace), CancellationToken.None)),
            workspaceOption, forkRepoArg, forkNameOption, forkWorkspaceOption);
        command.AddCommand(forkCommand);

        var cloneCommand = new Command("clone", "Get clone URL for a repository");
        var cloneRepoArg = new Argument<string>("repository", "Repository to clone");
        var sshOption = new Option<bool>("--ssh", "Get SSH URL instead of HTTPS");
        cloneCommand.AddArgument(cloneRepoArg);
        cloneCommand.AddOption(sshOption);
        cloneCommand.SetHandler((string? workspace, string repository, bool ssh) =>
            CommandRunner.RunRawAsync(() =>
                services.GetRequiredService<CloneRepoHandler>()
                    .HandleAsync(new CloneRepoRequest(workspace, repository, ssh), CancellationToken.None)),
            workspaceOption, cloneRepoArg, sshOption);
        command.AddCommand(cloneCommand);

        var permissionsCommand = new Command("permissions", "View repository permissions");
        var permRepoArg = new Argument<string>("repository", "Repository");
        permissionsCommand.AddArgument(permRepoArg);
        permissionsCommand.SetHandler((string? workspace, string repository) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<RepoPermissionsHandler>()
                    .HandleAsync(new RepoPermissionsRequest(workspace, repository), CancellationToken.None)),
            workspaceOption, permRepoArg);
        command.AddCommand(permissionsCommand);

        command.AddCommand(CreateHooksCommand(services, workspaceOption));

        return command;
    }

    private static Command CreateHooksCommand(IServiceProvider services, Option<string?> workspaceOption)
    {
        var hooksCommand = new Command("hooks", "Manage repository webhooks");
        var repoOption = CommandOptions.CreateRepoOption();
        hooksCommand.AddGlobalOption(repoOption);

        var listCommand = new Command("list", "List repository webhooks");
        var limitOption = new Option<int>("--limit", () => 25, "Maximum webhooks to list");
        listCommand.AddOption(limitOption);
        listCommand.SetHandler((string? workspace, string? repo, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListRepoHooksHandler>()
                    .HandleAsync(new ListRepoHooksRequest(workspace, repo, limit), CancellationToken.None)),
            workspaceOption, repoOption, limitOption);
        hooksCommand.AddCommand(listCommand);

        var viewCommand = new Command("view", "View a repository webhook");
        var viewUidArg = new Argument<string>("uid", "Webhook UUID");
        viewCommand.AddArgument(viewUidArg);
        viewCommand.SetHandler((string? workspace, string? repo, string uid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewRepoHookHandler>()
                    .HandleAsync(new ViewRepoHookRequest(workspace, repo, uid), CancellationToken.None)),
            workspaceOption, repoOption, viewUidArg);
        hooksCommand.AddCommand(viewCommand);

        var createCommand = new Command("create", "Create a repository webhook");
        var urlOption = new Option<string>("--url", "Webhook target URL") { IsRequired = true };
        var descriptionOption = new Option<string?>("--description", "Webhook description");
        var eventsOption = new Option<string[]?>("--events", "Events to trigger webhook (default: repo:push)");
        var activeOption = new Option<bool>("--active", () => true, "Whether the webhook is active");
        createCommand.AddOption(urlOption);
        createCommand.AddOption(descriptionOption);
        createCommand.AddOption(eventsOption);
        createCommand.AddOption(activeOption);
        createCommand.SetHandler((string? workspace, string? repo, string url, string? description, string[]? events, bool active) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateRepoHookHandler>()
                    .HandleAsync(new CreateRepoHookRequest(workspace, repo, url, description, events, active), CancellationToken.None)),
            workspaceOption, repoOption, urlOption, descriptionOption, eventsOption, activeOption);
        hooksCommand.AddCommand(createCommand);

        var updateCommand = new Command("update", "Update a repository webhook");
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
        updateCommand.SetHandler((string? workspace, string? repo, string uid, string? url, string? description, string[]? events, bool? active) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateRepoHookHandler>()
                    .HandleAsync(new UpdateRepoHookRequest(workspace, repo, uid, url, description, events, active), CancellationToken.None)),
            workspaceOption, repoOption, updateUidArg, updateUrlOption, updateDescriptionOption, updateEventsOption, updateActiveOption);
        hooksCommand.AddCommand(updateCommand);

        var deleteCommand = new Command("delete", "Delete a repository webhook");
        var deleteUidArg = new Argument<string>("uid", "Webhook UUID");
        var yesOption = new Option<bool>("--yes", "Skip confirmation");
        deleteCommand.AddArgument(deleteUidArg);
        deleteCommand.AddOption(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string? repo, string uid, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete webhook '{uid}'? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteRepoHookHandler>()
                    .HandleAsync(new DeleteRepoHookRequest(workspace, repo, uid), CancellationToken.None));
        }, workspaceOption, repoOption, deleteUidArg, yesOption);
        hooksCommand.AddCommand(deleteCommand);

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
