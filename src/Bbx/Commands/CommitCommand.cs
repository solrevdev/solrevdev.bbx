using System.CommandLine;
using Bbx.Features.Commits.ApproveCommit;
using Bbx.Features.Commits.CommitDiff;
using Bbx.Features.Commits.CommitDiffstat;
using Bbx.Features.Commits.CommitPatch;
using Bbx.Features.Commits.Comments.AddCommitComment;
using Bbx.Features.Commits.Comments.DeleteCommitComment;
using Bbx.Features.Commits.Comments.UpdateCommitComment;
using Bbx.Features.Commits.Comments.ViewCommitComment;
using Bbx.Features.Commits.CreateCommitStatus;
using Bbx.Features.Commits.FileHistory;
using Bbx.Features.Commits.ListCommitComments;
using Bbx.Features.Commits.ListCommitPullRequests;
using Bbx.Features.Commits.ListCommits;
using Bbx.Features.Commits.ListCommitStatuses;
using Bbx.Features.Commits.MergeBase;
using Bbx.Features.Commits.UnapproveCommit;
using Bbx.Features.Commits.UpdateCommitStatus;
using Bbx.Features.Commits.ViewCommit;
using Bbx.Features.Commits.ViewCommitStatus;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class CommitCommand
{
    public static Command Create(IServiceProvider services)
    {
        var workspaceOption = CommandOptions.CreateWorkspaceOption();
        var repoOption = CommandOptions.CreateRepoOption();
        var command = new Command("commit", "View commits and commit details");
        command.AddRecursiveOption(workspaceOption);
        command.AddRecursiveOption(repoOption);

        var listCommand = new Command("list", "List commits");
        var branchOption = new Option<string?>("--branch") { Description = "Filter by branch name" };
        var pathOption = new Option<string?>("--path") { Description = "Filter by file path" };
        var limitOption = new Option<int>("--limit") { Description = "Maximum commits to list", DefaultValueFactory = _ => 25 };
        listCommand.Options.Add(branchOption);
        listCommand.Options.Add(pathOption);
        var includeOption = new Option<string[]?>("--include")
        { Description = "Walk commits reachable from these refs. Repeatable. Switches to the POST form of the endpoint." };
        var excludeOption = new Option<string[]?>("--exclude")
        { Description = "Stop at commits reachable from these refs. Repeatable." };
        listCommand.Options.Add(limitOption);
        listCommand.Options.Add(includeOption);
        listCommand.Options.Add(excludeOption);
        listCommand.SetHandler((string? workspace, string? repo, string? branch, string? path, int limit, string[]? include, string[]? exclude) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListCommitsHandler>()
                    .HandleAsync(new ListCommitsRequest(workspace, repo, branch, path, limit, include, exclude), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, branchOption, pathOption, limitOption, includeOption, excludeOption);
        command.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View commit details");
        var hashArg = new Argument<string>("hash") { Description = "Commit hash" };
        viewCommand.Arguments.Add(hashArg);
        viewCommand.SetHandler((string? workspace, string? repo, string hash) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewCommitHandler>()
                    .HandleAsync(new ViewCommitRequest(workspace, repo, hash), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, hashArg);
        command.Subcommands.Add(viewCommand);

        var diffCommand = new Command("diff", "Show commit diff");
        var diffHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        diffCommand.Arguments.Add(diffHashArg);
        diffCommand.SetHandler((string? workspace, string? repo, string hash) =>
            CommandRunner.RunRawAsync(() =>
                services.GetRequiredService<CommitDiffHandler>()
                    .HandleAsync(new CommitDiffRequest(workspace, repo, hash), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, diffHashArg);
        command.Subcommands.Add(diffCommand);

        var patchCommand = new Command("patch", "Show commit as patch");
        var patchHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        patchCommand.Arguments.Add(patchHashArg);
        patchCommand.SetHandler((string? workspace, string? repo, string hash) =>
            CommandRunner.RunRawAsync(() =>
                services.GetRequiredService<CommitPatchHandler>()
                    .HandleAsync(new CommitPatchRequest(workspace, repo, hash), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, patchHashArg);
        command.Subcommands.Add(patchCommand);

        var commentsCommand = new Command("comments", "List commit comments");
        var commentsHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        commentsCommand.Arguments.Add(commentsHashArg);
        commentsCommand.SetHandler((string? workspace, string? repo, string hash) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListCommitCommentsHandler>()
                    .HandleAsync(new ListCommitCommentsRequest(workspace, repo, hash), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, commentsHashArg);
        command.Subcommands.Add(commentsCommand);

        var statusesCommand = new Command("statuses", "List commit build statuses");
        var statusesHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        statusesCommand.Arguments.Add(statusesHashArg);
        statusesCommand.SetHandler((string? workspace, string? repo, string hash) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListCommitStatusesHandler>()
                    .HandleAsync(new ListCommitStatusesRequest(workspace, repo, hash), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, statusesHashArg);
        command.Subcommands.Add(statusesCommand);

        var commentCommand = new Command("comment", "Add a comment to a commit");
        var commentHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var commentBodyOption = new Option<string>("--body") { Description = "Comment text", Required = true };
        var commentPathOption = new Option<string?>("--path") { Description = "Anchor the comment to this file" };
        var commentLineOption = new Option<int?>("--line") { Description = "Anchor the comment to this line of --path" };
        commentCommand.Arguments.Add(commentHashArg);
        commentCommand.Options.Add(commentBodyOption);
        commentCommand.Options.Add(commentPathOption);
        commentCommand.Options.Add(commentLineOption);
        commentCommand.SetHandler((string? workspace, string? repo, string hash, string body, string? path, int? line) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddCommitCommentHandler>()
                    .HandleAsync(new AddCommitCommentRequest(workspace, repo, hash, body, path, line), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, commentHashArg, commentBodyOption, commentPathOption, commentLineOption);
        command.Subcommands.Add(commentCommand);

        var commentViewCommand = new Command("comment-view", "View a single commit comment");
        var cvHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var cvCommentOption = new Option<int>("--comment-id") { Description = "Comment ID", Required = true };
        commentViewCommand.Arguments.Add(cvHashArg);
        commentViewCommand.Options.Add(cvCommentOption);
        commentViewCommand.SetHandler((string? workspace, string? repo, string hash, int commentId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewCommitCommentHandler>()
                    .HandleAsync(new ViewCommitCommentRequest(workspace, repo, hash, commentId), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, cvHashArg, cvCommentOption);
        command.Subcommands.Add(commentViewCommand);

        var commentUpdateCommand = new Command("comment-update", "Edit a commit comment");
        var cuHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var cuCommentOption = new Option<int>("--comment-id") { Description = "Comment ID", Required = true };
        var cuBodyOption = new Option<string>("--body") { Description = "Replacement comment text", Required = true };
        commentUpdateCommand.Arguments.Add(cuHashArg);
        commentUpdateCommand.Options.Add(cuCommentOption);
        commentUpdateCommand.Options.Add(cuBodyOption);
        commentUpdateCommand.SetHandler((string? workspace, string? repo, string hash, int commentId, string body) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateCommitCommentHandler>()
                    .HandleAsync(new UpdateCommitCommentRequest(workspace, repo, hash, commentId, body), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, cuHashArg, cuCommentOption, cuBodyOption);
        command.Subcommands.Add(commentUpdateCommand);

        var commentDeleteCommand = new Command("comment-delete", "Delete a commit comment");
        var cdHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var cdCommentOption = new Option<int>("--comment-id") { Description = "Comment ID", Required = true };
        var cdYesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        commentDeleteCommand.Arguments.Add(cdHashArg);
        commentDeleteCommand.Options.Add(cdCommentOption);
        commentDeleteCommand.Options.Add(cdYesOption);
        commentDeleteCommand.SetHandler(async (string? workspace, string? repo, string hash, int commentId, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr(
                    $"Delete comment #{commentId} on commit {hash}? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteCommitCommentHandler>()
                    .HandleAsync(new DeleteCommitCommentRequest(workspace, repo, hash, commentId), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, cdHashArg, cdCommentOption, cdYesOption);
        command.Subcommands.Add(commentDeleteCommand);

        command.Subcommands.Add(CreateStatusCommand(services, workspaceOption, repoOption));

        var filehistoryCommand = new Command("filehistory",
            "List commits that touched a file (the commit hash is the starting point)");
        var fhHashArg = new Argument<string>("hash") { Description = "Starting commit hash" };
        var fhPathArg = new Argument<string>("path") { Description = "File path" };
        var fhLimitOption = new Option<int>("--limit") { Description = "Maximum entries to list", DefaultValueFactory = _ => 25 };
        filehistoryCommand.Arguments.Add(fhHashArg);
        filehistoryCommand.Arguments.Add(fhPathArg);
        filehistoryCommand.Options.Add(fhLimitOption);
        filehistoryCommand.SetHandler((string? workspace, string? repo, string hash, string path, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<FileHistoryHandler>()
                    .HandleAsync(new FileHistoryRequest(workspace, repo, hash, path, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, fhHashArg, fhPathArg, fhLimitOption);
        command.Subcommands.Add(filehistoryCommand);

        var mergeBaseCommand = new Command("merge-base",
            "Find the merge-base commit for a spec (e.g., 'feature..main')");
        var mbSpecArg = new Argument<string>("spec") { Description = "Commit spec, e.g. 'feature..main' or 'abc..def'" };
        mergeBaseCommand.Arguments.Add(mbSpecArg);
        mergeBaseCommand.SetHandler((string? workspace, string? repo, string spec) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<MergeBaseHandler>()
                    .HandleAsync(new MergeBaseRequest(workspace, repo, spec), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, mbSpecArg);
        command.Subcommands.Add(mergeBaseCommand);

        var approveCommand = new Command("approve", "Approve a commit");
        var approveHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        approveCommand.Arguments.Add(approveHashArg);
        approveCommand.SetHandler((string? workspace, string? repo, string hash) =>
            CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<ApproveCommitHandler>()
                    .HandleAsync(new ApproveCommitRequest(workspace, repo, hash), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, approveHashArg);
        command.Subcommands.Add(approveCommand);

        var unapproveCommand = new Command("unapprove", "Remove approval from a commit");
        var unapproveHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        unapproveCommand.Arguments.Add(unapproveHashArg);
        unapproveCommand.SetHandler((string? workspace, string? repo, string hash) =>
            CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<UnapproveCommitHandler>()
                    .HandleAsync(new UnapproveCommitRequest(workspace, repo, hash), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, unapproveHashArg);
        command.Subcommands.Add(unapproveCommand);

        var diffstatCommand = new Command("diffstat",
            "Show per-file added/removed line counts for a spec (commit, branch, or 'src..dst')");
        var dsSpecArg = new Argument<string>("spec") { Description = "Commit hash, branch, or 'src..dst' range" };
        var dsLimitOption = new Option<int>("--limit") { Description = "Maximum file entries to list", DefaultValueFactory = _ => 100 };
        diffstatCommand.Arguments.Add(dsSpecArg);
        diffstatCommand.Options.Add(dsLimitOption);
        diffstatCommand.SetHandler((string? workspace, string? repo, string spec, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CommitDiffstatHandler>()
                    .HandleAsync(new CommitDiffstatRequest(workspace, repo, spec, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, dsSpecArg, dsLimitOption);
        command.Subcommands.Add(diffstatCommand);

        var prsCommand = new Command("pullrequests", "List pull requests for a commit");
        var prsHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        prsCommand.Arguments.Add(prsHashArg);
        prsCommand.SetHandler((string? workspace, string? repo, string hash) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListCommitPullRequestsHandler>()
                    .HandleAsync(new ListCommitPullRequestsRequest(workspace, repo, hash), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, prsHashArg);
        command.Subcommands.Add(prsCommand);

        return command;
    }

    private static Command CreateStatusCommand(IServiceProvider services, Option<string?> workspaceOption, Option<string?> repoOption)
    {
        var statusCommand = new Command("status", "Create or update commit build statuses");

        var createCommand = new Command("create", "Create a build status on a commit");
        var createHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var createKeyOption = new Option<string>("--key") { Description = "Build status key", Required = true };
        var createStateOption = new Option<string>("--state")
        { Description = "Build state (SUCCESSFUL, FAILED, INPROGRESS, STOPPED)", Required = true };
        var createUrlOption = new Option<string>("--url") { Description = "URL to the build (e.g., CI run)", Required = true };
        var createNameOption = new Option<string?>("--name") { Description = "Human-readable name" };
        var createDescriptionOption = new Option<string?>("--description") { Description = "Description" };
        createCommand.Arguments.Add(createHashArg);
        createCommand.Options.Add(createKeyOption);
        createCommand.Options.Add(createStateOption);
        createCommand.Options.Add(createUrlOption);
        createCommand.Options.Add(createNameOption);
        createCommand.Options.Add(createDescriptionOption);
        createCommand.SetHandler((string? workspace, string? repo, string hash, string key, string state, string url, string? name, string? description) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateCommitStatusHandler>()
                    .HandleAsync(new CreateCommitStatusRequest(workspace, repo, hash, key, state, url, name, description), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, createHashArg, createKeyOption, createStateOption, createUrlOption, createNameOption, createDescriptionOption);
        statusCommand.Subcommands.Add(createCommand);

        var viewCommand = new Command("view", "View one build status on a commit by its key");
        var viewHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var viewKeyOption = new Option<string>("--key") { Description = "Build status key", Required = true };
        viewCommand.Arguments.Add(viewHashArg);
        viewCommand.Options.Add(viewKeyOption);
        viewCommand.SetHandler((string? workspace, string? repo, string hash, string key) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewCommitStatusHandler>()
                    .HandleAsync(new ViewCommitStatusRequest(workspace, repo, hash, key), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, viewHashArg, viewKeyOption);
        statusCommand.Subcommands.Add(viewCommand);

        var updateCommand = new Command("update", "Update an existing build status on a commit");
        var updateHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var updateKeyOption = new Option<string>("--key") { Description = "Build status key", Required = true };
        var updateStateOption = new Option<string?>("--state")
        { Description = "Build state (SUCCESSFUL, FAILED, INPROGRESS, STOPPED)" };
        var updateUrlOption = new Option<string?>("--url") { Description = "URL to the build" };
        var updateNameOption = new Option<string?>("--name") { Description = "Human-readable name" };
        var updateDescriptionOption = new Option<string?>("--description") { Description = "Description" };
        updateCommand.Arguments.Add(updateHashArg);
        updateCommand.Options.Add(updateKeyOption);
        updateCommand.Options.Add(updateStateOption);
        updateCommand.Options.Add(updateUrlOption);
        updateCommand.Options.Add(updateNameOption);
        updateCommand.Options.Add(updateDescriptionOption);
        updateCommand.SetHandler((string? workspace, string? repo, string hash, string key, string? state, string? url, string? name, string? description) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateCommitStatusHandler>()
                    .HandleAsync(new UpdateCommitStatusRequest(workspace, repo, hash, key, state, url, name, description), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, updateHashArg, updateKeyOption, updateStateOption, updateUrlOption, updateNameOption, updateDescriptionOption);
        statusCommand.Subcommands.Add(updateCommand);

        return statusCommand;
    }
}
