using System.CommandLine;
using Bbx.Features.Commits.CommitDiff;
using Bbx.Features.Commits.CommitPatch;
using Bbx.Features.Commits.ListCommitComments;
using Bbx.Features.Commits.ListCommitPullRequests;
using Bbx.Features.Commits.ListCommitStatuses;
using Bbx.Features.Commits.ListCommits;
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
}
