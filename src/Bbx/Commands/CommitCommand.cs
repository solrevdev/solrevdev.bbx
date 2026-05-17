using System.CommandLine;
using Bbx.Features.Commits.CommitDiff;
using Bbx.Features.Commits.CommitPatch;
using Bbx.Features.Commits.CreateCommitStatus;
using Bbx.Features.Commits.ListCommitComments;
using Bbx.Features.Commits.ListCommitPullRequests;
using Bbx.Features.Commits.ListCommitStatuses;
using Bbx.Features.Commits.ListCommits;
using Bbx.Features.Commits.UpdateCommitStatus;
using Bbx.Features.Commits.ViewCommit;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class CommitCommand
{
    public static Command Create(IServiceProvider services)
    {
        var workspaceOption = CommandOptions.CreateWorkspaceOption();
        var repoOption = CommandOptions.CreateRepoOption();
        var command = new Command("commit", "View commits and commit details");
        command.AddGlobalOption(workspaceOption);
        command.AddGlobalOption(repoOption);

        var listCommand = new Command("list", "List commits");
        var branchOption = new Option<string?>("--branch", "Filter by branch name");
        var pathOption = new Option<string?>("--path", "Filter by file path");
        var limitOption = new Option<int>("--limit", () => 25, "Maximum commits to list");
        listCommand.AddOption(branchOption);
        listCommand.AddOption(pathOption);
        listCommand.AddOption(limitOption);
        listCommand.SetHandler((string? workspace, string? repo, string? branch, string? path, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListCommitsHandler>()
                    .HandleAsync(new ListCommitsRequest(workspace, repo, branch, path, limit), CancellationToken.None)),
            workspaceOption, repoOption, branchOption, pathOption, limitOption);
        command.AddCommand(listCommand);

        var viewCommand = new Command("view", "View commit details");
        var hashArg = new Argument<string>("hash", "Commit hash");
        viewCommand.AddArgument(hashArg);
        viewCommand.SetHandler((string? workspace, string? repo, string hash) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewCommitHandler>()
                    .HandleAsync(new ViewCommitRequest(workspace, repo, hash), CancellationToken.None)),
            workspaceOption, repoOption, hashArg);
        command.AddCommand(viewCommand);

        var diffCommand = new Command("diff", "Show commit diff");
        var diffHashArg = new Argument<string>("hash", "Commit hash");
        diffCommand.AddArgument(diffHashArg);
        diffCommand.SetHandler((string? workspace, string? repo, string hash) =>
            CommandRunner.RunRawAsync(() =>
                services.GetRequiredService<CommitDiffHandler>()
                    .HandleAsync(new CommitDiffRequest(workspace, repo, hash), CancellationToken.None)),
            workspaceOption, repoOption, diffHashArg);
        command.AddCommand(diffCommand);

        var patchCommand = new Command("patch", "Show commit as patch");
        var patchHashArg = new Argument<string>("hash", "Commit hash");
        patchCommand.AddArgument(patchHashArg);
        patchCommand.SetHandler((string? workspace, string? repo, string hash) =>
            CommandRunner.RunRawAsync(() =>
                services.GetRequiredService<CommitPatchHandler>()
                    .HandleAsync(new CommitPatchRequest(workspace, repo, hash), CancellationToken.None)),
            workspaceOption, repoOption, patchHashArg);
        command.AddCommand(patchCommand);

        var commentsCommand = new Command("comments", "List commit comments");
        var commentsHashArg = new Argument<string>("hash", "Commit hash");
        commentsCommand.AddArgument(commentsHashArg);
        commentsCommand.SetHandler((string? workspace, string? repo, string hash) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListCommitCommentsHandler>()
                    .HandleAsync(new ListCommitCommentsRequest(workspace, repo, hash), CancellationToken.None)),
            workspaceOption, repoOption, commentsHashArg);
        command.AddCommand(commentsCommand);

        var statusesCommand = new Command("statuses", "List commit build statuses");
        var statusesHashArg = new Argument<string>("hash", "Commit hash");
        statusesCommand.AddArgument(statusesHashArg);
        statusesCommand.SetHandler((string? workspace, string? repo, string hash) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListCommitStatusesHandler>()
                    .HandleAsync(new ListCommitStatusesRequest(workspace, repo, hash), CancellationToken.None)),
            workspaceOption, repoOption, statusesHashArg);
        command.AddCommand(statusesCommand);

        command.AddCommand(CreateStatusCommand(services, workspaceOption, repoOption));

        var prsCommand = new Command("pullrequests", "List pull requests for a commit");
        var prsHashArg = new Argument<string>("hash", "Commit hash");
        prsCommand.AddArgument(prsHashArg);
        prsCommand.SetHandler((string? workspace, string? repo, string hash) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListCommitPullRequestsHandler>()
                    .HandleAsync(new ListCommitPullRequestsRequest(workspace, repo, hash), CancellationToken.None)),
            workspaceOption, repoOption, prsHashArg);
        command.AddCommand(prsCommand);

        return command;
    }

    private static Command CreateStatusCommand(IServiceProvider services, Option<string?> workspaceOption, Option<string?> repoOption)
    {
        var statusCommand = new Command("status", "Create or update commit build statuses");

        var createCommand = new Command("create", "Create a build status on a commit");
        var createHashArg = new Argument<string>("hash", "Commit hash");
        var createKeyOption = new Option<string>("--key", "Build status key") { IsRequired = true };
        var createStateOption = new Option<string>("--state",
            "Build state (SUCCESSFUL, FAILED, INPROGRESS, STOPPED)") { IsRequired = true };
        var createUrlOption = new Option<string>("--url", "URL to the build (e.g., CI run)") { IsRequired = true };
        var createNameOption = new Option<string?>("--name", "Human-readable name");
        var createDescriptionOption = new Option<string?>("--description", "Description");
        createCommand.AddArgument(createHashArg);
        createCommand.AddOption(createKeyOption);
        createCommand.AddOption(createStateOption);
        createCommand.AddOption(createUrlOption);
        createCommand.AddOption(createNameOption);
        createCommand.AddOption(createDescriptionOption);
        createCommand.SetHandler((string? workspace, string? repo, string hash, string key, string state, string url, string? name, string? description) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateCommitStatusHandler>()
                    .HandleAsync(new CreateCommitStatusRequest(workspace, repo, hash, key, state, url, name, description), CancellationToken.None)),
            workspaceOption, repoOption, createHashArg, createKeyOption, createStateOption, createUrlOption, createNameOption, createDescriptionOption);
        statusCommand.AddCommand(createCommand);

        var updateCommand = new Command("update", "Update an existing build status on a commit");
        var updateHashArg = new Argument<string>("hash", "Commit hash");
        var updateKeyOption = new Option<string>("--key", "Build status key") { IsRequired = true };
        var updateStateOption = new Option<string?>("--state",
            "Build state (SUCCESSFUL, FAILED, INPROGRESS, STOPPED)");
        var updateUrlOption = new Option<string?>("--url", "URL to the build");
        var updateNameOption = new Option<string?>("--name", "Human-readable name");
        var updateDescriptionOption = new Option<string?>("--description", "Description");
        updateCommand.AddArgument(updateHashArg);
        updateCommand.AddOption(updateKeyOption);
        updateCommand.AddOption(updateStateOption);
        updateCommand.AddOption(updateUrlOption);
        updateCommand.AddOption(updateNameOption);
        updateCommand.AddOption(updateDescriptionOption);
        updateCommand.SetHandler((string? workspace, string? repo, string hash, string key, string? state, string? url, string? name, string? description) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateCommitStatusHandler>()
                    .HandleAsync(new UpdateCommitStatusRequest(workspace, repo, hash, key, state, url, name, description), CancellationToken.None)),
            workspaceOption, repoOption, updateHashArg, updateKeyOption, updateStateOption, updateUrlOption, updateNameOption, updateDescriptionOption);
        statusCommand.AddCommand(updateCommand);

        return statusCommand;
    }
}
