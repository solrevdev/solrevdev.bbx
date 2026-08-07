using System.CommandLine;
using Bbx.Features.PullRequests.AddPullRequestComment;
using Bbx.Features.PullRequests.ApprovePullRequest;
using Bbx.Features.PullRequests.Comments.DeletePullRequestComment;
using Bbx.Features.PullRequests.Comments.ResolvePullRequestComment;
using Bbx.Features.PullRequests.Comments.UpdatePullRequestComment;
using Bbx.Features.PullRequests.Comments.ViewPullRequestComment;
using Bbx.Features.PullRequests.CreatePullRequest;
using Bbx.Features.PullRequests.DeclinePullRequest;
using Bbx.Features.PullRequests.ListPullRequestComments;
using Bbx.Features.PullRequests.ListPullRequestCommits;
using Bbx.Features.PullRequests.ListPullRequests;
using Bbx.Features.PullRequests.MergePullRequest;
using Bbx.Features.PullRequests.PullRequestActivity;
using Bbx.Features.PullRequests.PullRequestConflicts;
using Bbx.Features.PullRequests.PullRequestDiff;
using Bbx.Features.PullRequests.PullRequestDiffstat;
using Bbx.Features.PullRequests.PullRequestMergeStatus;
using Bbx.Features.PullRequests.PullRequestPatch;
using Bbx.Features.PullRequests.PullRequestStatuses;
using Bbx.Features.PullRequests.RequestChanges;
using Bbx.Features.PullRequests.Tasks.AddPullRequestTask;
using Bbx.Features.PullRequests.Tasks.DeletePullRequestTask;
using Bbx.Features.PullRequests.Tasks.ListPullRequestTasks;
using Bbx.Features.PullRequests.Tasks.UpdatePullRequestTask;
using Bbx.Features.PullRequests.Tasks.ViewPullRequestTask;
using Bbx.Features.PullRequests.UnapprovePullRequest;
using Bbx.Features.PullRequests.UpdatePullRequest;
using Bbx.Features.PullRequests.UnrequestChanges;
using Bbx.Features.PullRequests.ViewPullRequest;
using Bbx.Features.Repos.DefaultReviewers.EffectiveDefaultReviewers;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class PrCommand
{
    public static Command Create(IServiceProvider services)
    {
        var workspaceOption = CommandOptions.CreateWorkspaceOption();
        var repoOption = CommandOptions.CreateRepoOption();
        var command = new Command("pr", "Manage pull requests");
        command.AddRecursiveOption(workspaceOption);
        command.AddRecursiveOption(repoOption);

        var listCommand = new Command("list", "List pull requests");
        var stateOption = new Option<string?>("--state") { Description = "Filter by state (OPEN, MERGED, DECLINED, SUPERSEDED)" };
        var authorOption = new Option<string?>("--author") { Description = "Filter by author account ID" };
        var limitOption = new Option<int>("--limit") { Description = "Maximum PRs to list", DefaultValueFactory = _ => 25 };
        listCommand.Options.Add(stateOption);
        listCommand.Options.Add(authorOption);
        listCommand.Options.Add(limitOption);
        listCommand.SetHandler((string? workspace, string? repo, string? state, string? author, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListPullRequestsHandler>()
                    .HandleAsync(new ListPullRequestsRequest(workspace, repo, state, author, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, stateOption, authorOption, limitOption);
        command.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View pull request details");
        var idArg = new Argument<int>("id") { Description = "Pull request ID" };
        viewCommand.Arguments.Add(idArg);
        viewCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewPullRequestHandler>()
                    .HandleAsync(new ViewPullRequestRequest(workspace, repo, id), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, idArg);
        command.Subcommands.Add(viewCommand);

        var createCommand = new Command("create", "Create a new pull request");
        var titleOption = new Option<string>("--title") { Description = "Pull request title", Required = true };
        var sourceOption = new Option<string>("--source") { Description = "Source branch", Required = true };
        var destOption = new Option<string>("--dest") { Description = "Destination branch", Required = true };
        var bodyOption = new Option<string?>("--body") { Description = "Pull request description" };
        var reviewersOption = new Option<string[]?>("--reviewers") { Description = "Reviewer account IDs (UUID format)" };
        var closeSourceOption = new Option<bool>("--close-source-branch")
        {
            Description = "Ask Bitbucket to close the source branch when this pull request merges. "
                          + "Server-side only: no local clone is touched.",
        };
        createCommand.Options.Add(titleOption);
        createCommand.Options.Add(sourceOption);
        createCommand.Options.Add(destOption);
        createCommand.Options.Add(bodyOption);
        createCommand.Options.Add(reviewersOption);
        createCommand.Options.Add(closeSourceOption);
        createCommand.SetHandler((string? workspace, string? repo, string title, string source, string dest, string? body, string[]? reviewers, bool closeSource) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreatePullRequestHandler>()
                    .HandleAsync(new CreatePullRequestRequest(workspace, repo, title, source, dest, body, reviewers, closeSource), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, titleOption, sourceOption, destOption, bodyOption, reviewersOption, closeSourceOption);
        command.Subcommands.Add(createCommand);

        var updateCommand = new Command("update", "Update an open pull request");
        var updateIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var updateTitleOption = new Option<string?>("--title") { Description = "New title" };
        var updateBodyOption = new Option<string?>("--body") { Description = "New description. Pass an empty string to clear it." };
        var updateDestOption = new Option<string?>("--dest") { Description = "Retarget the pull request at this destination branch" };
        var updateReviewersOption = new Option<string[]?>("--reviewers") { Description = "Replace reviewers with these account IDs" };
        var updateCloseSourceOption = new Option<bool>("--close-source-branch")
        {
            Description = "Close the source branch on Bitbucket when this pull request merges. "
                          + "Needs another real change in the same call to stick. No local clone is touched.",
        };
        var updateKeepSourceOption = new Option<bool>("--no-close-source-branch")
        { Description = "Keep the source branch on Bitbucket after the merge" };
        updateCommand.Arguments.Add(updateIdArg);
        updateCommand.Options.Add(updateTitleOption);
        updateCommand.Options.Add(updateBodyOption);
        updateCommand.Options.Add(updateDestOption);
        updateCommand.Options.Add(updateReviewersOption);
        updateCommand.Options.Add(updateCloseSourceOption);
        updateCommand.Options.Add(updateKeepSourceOption);
        updateCommand.SetHandler((string? workspace, string? repo, int id, string? title, string? body, string? dest, string[]? reviewers, bool closeSource, bool keepSource) =>
            CommandRunner.RunJsonAsync(() =>
            {
                if (closeSource && keepSource)
                    throw new BbxUserException(
                        "Error: --close-source-branch and --no-close-source-branch are mutually exclusive.");
                bool? closeSourceBranch = closeSource ? true : keepSource ? false : null;
                return services.GetRequiredService<UpdatePullRequestHandler>()
                    .HandleAsync(new UpdatePullRequestRequest(workspace, repo, id, title, body, dest, reviewers, closeSourceBranch), CommandBinding.CancellationToken);
            }),
            workspaceOption, repoOption, updateIdArg, updateTitleOption, updateBodyOption, updateDestOption, updateReviewersOption, updateCloseSourceOption, updateKeepSourceOption);
        command.Subcommands.Add(updateCommand);

        var mergeCommand = new Command("merge", "Merge a pull request");
        var mergeIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        // No default: the destination branch has one, and it is allowed to
        // differ from Bitbucket's. Left off, bbx uses whatever that branch says.
        var strategyOption = new Option<string?>("--strategy")
        {
            Description = "Merge strategy: merge_commit, squash, fast_forward, squash_fast_forward, "
                          + "rebase_fast_forward or rebase_merge. 'merge' is accepted for merge_commit. "
                          + "Defaults to the destination branch's own default strategy.",
        };
        var messageOption = new Option<string?>("--message") { Description = "Merge commit message" };
        // gh spells the nearest thing `-d, --delete-branch` and it deletes the
        // local branch too. This one cannot: it is a field on Bitbucket's merge
        // endpoint, executed on Bitbucket. Say so here, because the help text is
        // where that assumption gets made.
        var closeSourceMergeOption = new Option<bool>("--close-source-branch")
        {
            Description = "Close the source branch on Bitbucket after the merge. Server-side only: "
                          + "your local branch and its remote-tracking ref survive. Clear them with "
                          + "`git fetch origin --prune` then `git branch -d <branch>`.",
        };
        // Merging writes to the destination branch and cannot be undone from
        // here, so it confirms like the other destructive verbs.
        var mergeYesOption = new Option<bool>("--yes") { Description = "Skip confirmation prompt" };
        mergeCommand.Arguments.Add(mergeIdArg);
        mergeCommand.Options.Add(strategyOption);
        mergeCommand.Options.Add(messageOption);
        mergeCommand.Options.Add(closeSourceMergeOption);
        mergeCommand.Options.Add(mergeYesOption);
        mergeCommand.SetHandler(async (string? workspace, string? repo, int id, string? strategy, string? message, bool closeSource, bool yes) =>
        {
            var using_ = string.IsNullOrEmpty(strategy)
                ? "the destination branch's default strategy"
                : $"'{strategy}'";
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Merge PR #{id} using {using_}? [y/N]: "))
                return;
            await CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<MergePullRequestHandler>()
                    .HandleAsync(new MergePullRequestRequest(workspace, repo, id, strategy, message, closeSource), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, mergeIdArg, strategyOption, messageOption, closeSourceMergeOption, mergeYesOption);
        command.Subcommands.Add(mergeCommand);

        var approveCommand = new Command("approve", "Approve a pull request");
        var approveIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        approveCommand.Arguments.Add(approveIdArg);
        approveCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<ApprovePullRequestHandler>()
                    .HandleAsync(new ApprovePullRequestRequest(workspace, repo, id), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, approveIdArg);
        command.Subcommands.Add(approveCommand);

        var unapproveCommand = new Command("unapprove", "Remove approval from a pull request");
        var unapproveIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        unapproveCommand.Arguments.Add(unapproveIdArg);
        unapproveCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<UnapprovePullRequestHandler>()
                    .HandleAsync(new UnapprovePullRequestRequest(workspace, repo, id), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, unapproveIdArg);
        command.Subcommands.Add(unapproveCommand);

        var declineCommand = new Command("decline",
            "Decline a pull request. The source branch is left open; delete it with `bbx branch delete`.");
        var declineIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var declineReasonOption = new Option<string?>("--reason") { Description = "Reason for declining" };
        declineCommand.Arguments.Add(declineIdArg);
        declineCommand.Options.Add(declineReasonOption);
        declineCommand.SetHandler((string? workspace, string? repo, int id, string? reason) =>
            CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeclinePullRequestHandler>()
                    .HandleAsync(new DeclinePullRequestRequest(workspace, repo, id, reason), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, declineIdArg, declineReasonOption);
        command.Subcommands.Add(declineCommand);

        var commentsCommand = new Command("comments", "List pull request comments");
        var commentsIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        commentsCommand.Arguments.Add(commentsIdArg);
        commentsCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListPullRequestCommentsHandler>()
                    .HandleAsync(new ListPullRequestCommentsRequest(workspace, repo, id), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, commentsIdArg);
        command.Subcommands.Add(commentsCommand);

        var commentCommand = new Command("comment", "Add a comment to a pull request");
        var commentIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var commentBodyOption = new Option<string>("--body") { Description = "Comment text", Required = true };
        commentCommand.Arguments.Add(commentIdArg);
        commentCommand.Options.Add(commentBodyOption);
        commentCommand.SetHandler((string? workspace, string? repo, int id, string commentBody) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddPullRequestCommentHandler>()
                    .HandleAsync(new AddPullRequestCommentRequest(workspace, repo, id, commentBody), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, commentIdArg, commentBodyOption);
        command.Subcommands.Add(commentCommand);

        var diffCommand = new Command("diff", "Show pull request diff");
        var diffIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        diffCommand.Arguments.Add(diffIdArg);
        diffCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunRawAsync(() =>
                services.GetRequiredService<PullRequestDiffHandler>()
                    .HandleAsync(new PullRequestDiffRequest(workspace, repo, id), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, diffIdArg);
        command.Subcommands.Add(diffCommand);

        var activityCommand = new Command("activity",
            "Show pull request activity. With no ID, the whole repository's feed.");
        var activityIdArg = new Argument<int?>("id")
        {
            Description = "Pull request ID. Omit for repository-wide activity.",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => null,
        };
        var activityLimitOption = new Option<int>("--limit")
        { Description = "Maximum activity entries to list", DefaultValueFactory = _ => 50 };
        activityCommand.Arguments.Add(activityIdArg);
        activityCommand.Options.Add(activityLimitOption);
        activityCommand.SetHandler((string? workspace, string? repo, int? id, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<PullRequestActivityHandler>()
                    .HandleAsync(new PullRequestActivityRequest(workspace, repo, id, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, activityIdArg, activityLimitOption);
        command.Subcommands.Add(activityCommand);

        var conflictsCommand = new Command("conflicts", "List the files a merge of this PR would conflict on");
        var conflictsIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var conflictsLimitOption = new Option<int>("--limit")
        { Description = "Maximum conflicts to list", DefaultValueFactory = _ => 100 };
        conflictsCommand.Arguments.Add(conflictsIdArg);
        conflictsCommand.Options.Add(conflictsLimitOption);
        conflictsCommand.SetHandler((string? workspace, string? repo, int id, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<PullRequestConflictsHandler>()
                    .HandleAsync(new PullRequestConflictsRequest(workspace, repo, id, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, conflictsIdArg, conflictsLimitOption);
        command.Subcommands.Add(conflictsCommand);

        var diffstatCommand = new Command("diffstat", "Show per-file line counts for a pull request");
        var diffstatIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var diffstatLimitOption = new Option<int>("--limit")
        { Description = "Maximum files to list", DefaultValueFactory = _ => 500 };
        diffstatCommand.Arguments.Add(diffstatIdArg);
        diffstatCommand.Options.Add(diffstatLimitOption);
        diffstatCommand.SetHandler((string? workspace, string? repo, int id, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<PullRequestDiffstatHandler>()
                    .HandleAsync(new PullRequestDiffstatRequest(workspace, repo, id, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, diffstatIdArg, diffstatLimitOption);
        command.Subcommands.Add(diffstatCommand);

        var mergeStatusCommand = new Command("merge-status",
            "Report on a merge that was accepted asynchronously and returned a task ID");
        var mergeStatusIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var mergeStatusTaskOption = new Option<string>("--task-id")
        { Description = "Merge task ID, as returned by an asynchronous merge", Required = true };
        mergeStatusCommand.Arguments.Add(mergeStatusIdArg);
        mergeStatusCommand.Options.Add(mergeStatusTaskOption);
        mergeStatusCommand.SetHandler((string? workspace, string? repo, int id, string taskId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<PullRequestMergeStatusHandler>()
                    .HandleAsync(new PullRequestMergeStatusRequest(workspace, repo, id, taskId), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, mergeStatusIdArg, mergeStatusTaskOption);
        command.Subcommands.Add(mergeStatusCommand);

        var commentViewCommand = new Command("comment-view", "View a single pull request comment");
        var cvIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var cvCommentOption = new Option<int>("--comment-id") { Description = "Comment ID", Required = true };
        commentViewCommand.Arguments.Add(cvIdArg);
        commentViewCommand.Options.Add(cvCommentOption);
        commentViewCommand.SetHandler((string? workspace, string? repo, int id, int commentId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewPullRequestCommentHandler>()
                    .HandleAsync(new ViewPullRequestCommentRequest(workspace, repo, id, commentId), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, cvIdArg, cvCommentOption);
        command.Subcommands.Add(commentViewCommand);

        var commentUpdateCommand = new Command("comment-update", "Edit a pull request comment");
        var cuIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var cuCommentOption = new Option<int>("--comment-id") { Description = "Comment ID", Required = true };
        var cuBodyOption = new Option<string>("--body") { Description = "Replacement comment text", Required = true };
        commentUpdateCommand.Arguments.Add(cuIdArg);
        commentUpdateCommand.Options.Add(cuCommentOption);
        commentUpdateCommand.Options.Add(cuBodyOption);
        commentUpdateCommand.SetHandler((string? workspace, string? repo, int id, int commentId, string body) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdatePullRequestCommentHandler>()
                    .HandleAsync(new UpdatePullRequestCommentRequest(workspace, repo, id, commentId, body), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, cuIdArg, cuCommentOption, cuBodyOption);
        command.Subcommands.Add(commentUpdateCommand);

        var commentDeleteCommand = new Command("comment-delete", "Delete a pull request comment");
        var cdIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var cdCommentOption = new Option<int>("--comment-id") { Description = "Comment ID", Required = true };
        var cdYesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        commentDeleteCommand.Arguments.Add(cdIdArg);
        commentDeleteCommand.Options.Add(cdCommentOption);
        commentDeleteCommand.Options.Add(cdYesOption);
        commentDeleteCommand.SetHandler(async (string? workspace, string? repo, int id, int commentId, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr(
                    $"Delete comment #{commentId} on PR #{id}? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeletePullRequestCommentHandler>()
                    .HandleAsync(new DeletePullRequestCommentRequest(workspace, repo, id, commentId), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, cdIdArg, cdCommentOption, cdYesOption);
        command.Subcommands.Add(commentDeleteCommand);

        command.Subcommands.Add(CreateResolveCommand(services, workspaceOption, repoOption,
            "comment-resolve", "Mark a pull request comment as resolved", resolve: true));
        command.Subcommands.Add(CreateResolveCommand(services, workspaceOption, repoOption,
            "comment-unresolve", "Reopen a resolved pull request comment", resolve: false));

        var statusesCommand = new Command("statuses", "Show pull request commit statuses");
        var statusesIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        statusesCommand.Arguments.Add(statusesIdArg);
        statusesCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<PullRequestStatusesHandler>()
                    .HandleAsync(new PullRequestStatusesRequest(workspace, repo, id), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, statusesIdArg);
        command.Subcommands.Add(statusesCommand);

        // The "effective default reviewers" endpoint is repo-scoped (not
        // PR-scoped), so we don't take a PR id here. Surfacing it under `pr`
        // matches reviewer-workflow muscle memory while pointing at the
        // repo-level endpoint that actually returns the data.
        var defaultReviewersCommand = new Command("default-reviewers",
            "Show effective default reviewers for the repository (inherited from project + repo)");
        var drLimitOption = new Option<int>("--limit") { Description = "Maximum reviewers to list", DefaultValueFactory = _ => 25 };
        defaultReviewersCommand.Options.Add(drLimitOption);
        defaultReviewersCommand.SetHandler((string? workspace, string? repo, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<EffectiveDefaultReviewersHandler>()
                    .HandleAsync(new EffectiveDefaultReviewersRequest(workspace, repo, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, drLimitOption);
        command.Subcommands.Add(defaultReviewersCommand);

        command.Subcommands.Add(CreateTasksCommand(services, workspaceOption, repoOption));

        var requestChangesCommand = new Command("request-changes", "Mark PR as needing changes");
        var rcIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        requestChangesCommand.Arguments.Add(rcIdArg);
        requestChangesCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<RequestChangesHandler>()
                    .HandleAsync(new RequestChangesRequest(workspace, repo, id), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, rcIdArg);
        command.Subcommands.Add(requestChangesCommand);

        var unrequestChangesCommand = new Command("unrequest-changes", "Remove a previous request-changes review");
        var urcIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        unrequestChangesCommand.Arguments.Add(urcIdArg);
        unrequestChangesCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<UnrequestChangesHandler>()
                    .HandleAsync(new UnrequestChangesRequest(workspace, repo, id), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, urcIdArg);
        command.Subcommands.Add(unrequestChangesCommand);

        var commitsCommand = new Command("commits", "List commits in a pull request");
        var commitsIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var commitsLimitOption = new Option<int>("--limit") { Description = "Maximum commits to list", DefaultValueFactory = _ => 50 };
        commitsCommand.Arguments.Add(commitsIdArg);
        commitsCommand.Options.Add(commitsLimitOption);
        commitsCommand.SetHandler((string? workspace, string? repo, int id, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListPullRequestCommitsHandler>()
                    .HandleAsync(new ListPullRequestCommitsRequest(workspace, repo, id, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, commitsIdArg, commitsLimitOption);
        command.Subcommands.Add(commitsCommand);

        var patchCommand = new Command("patch", "Show PR as a git-format patch");
        var patchIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        patchCommand.Arguments.Add(patchIdArg);
        patchCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunRawAsync(() =>
                services.GetRequiredService<PullRequestPatchHandler>()
                    .HandleAsync(new PullRequestPatchRequest(workspace, repo, id), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, patchIdArg);
        command.Subcommands.Add(patchCommand);

        return command;
    }

    private static Command CreateTasksCommand(IServiceProvider services, Option<string?> workspaceOption, Option<string?> repoOption)
    {
        var tasksCommand = new Command("tasks", "Manage PR tasks");

        var listCommand = new Command("list", "List tasks on a PR");
        var listIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var listLimitOption = new Option<int>("--limit") { Description = "Maximum tasks to list", DefaultValueFactory = _ => 50 };
        listCommand.Arguments.Add(listIdArg);
        listCommand.Options.Add(listLimitOption);
        listCommand.SetHandler((string? workspace, string? repo, int id, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListPullRequestTasksHandler>()
                    .HandleAsync(new ListPullRequestTasksRequest(workspace, repo, id, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, listIdArg, listLimitOption);
        tasksCommand.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View a single task on a PR");
        var viewIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var viewTaskIdOption = new Option<int>("--task-id") { Description = "Task ID", Required = true };
        viewCommand.Arguments.Add(viewIdArg);
        viewCommand.Options.Add(viewTaskIdOption);
        viewCommand.SetHandler((string? workspace, string? repo, int id, int taskId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewPullRequestTaskHandler>()
                    .HandleAsync(new ViewPullRequestTaskRequest(workspace, repo, id, taskId), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, viewIdArg, viewTaskIdOption);
        tasksCommand.Subcommands.Add(viewCommand);

        var addCommand = new Command("add", "Add a task to a PR");
        var addIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var addContentOption = new Option<string>("--content") { Description = "Task body (markdown)", Required = true };
        addCommand.Arguments.Add(addIdArg);
        addCommand.Options.Add(addContentOption);
        addCommand.SetHandler((string? workspace, string? repo, int id, string content) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddPullRequestTaskHandler>()
                    .HandleAsync(new AddPullRequestTaskRequest(workspace, repo, id, content), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, addIdArg, addContentOption);
        tasksCommand.Subcommands.Add(addCommand);

        var updateCommand = new Command("update", "Update a PR task (content and/or state)");
        var updateIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var updateTaskIdOption = new Option<int>("--task-id") { Description = "Task ID", Required = true };
        var updateContentOption = new Option<string?>("--content") { Description = "New task body" };
        var updateStateOption = new Option<string?>("--state") { Description = "Task state (RESOLVED or UNRESOLVED)" };
        updateCommand.Arguments.Add(updateIdArg);
        updateCommand.Options.Add(updateTaskIdOption);
        updateCommand.Options.Add(updateContentOption);
        updateCommand.Options.Add(updateStateOption);
        updateCommand.SetHandler((string? workspace, string? repo, int id, int taskId, string? content, string? state) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdatePullRequestTaskHandler>()
                    .HandleAsync(new UpdatePullRequestTaskRequest(workspace, repo, id, taskId, content, state), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, updateIdArg, updateTaskIdOption, updateContentOption, updateStateOption);
        tasksCommand.Subcommands.Add(updateCommand);

        var completeCommand = new Command("complete", "Mark a PR task as RESOLVED");
        var completeIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var completeTaskIdOption = new Option<int>("--task-id") { Description = "Task ID", Required = true };
        completeCommand.Arguments.Add(completeIdArg);
        completeCommand.Options.Add(completeTaskIdOption);
        completeCommand.SetHandler((string? workspace, string? repo, int id, int taskId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdatePullRequestTaskHandler>()
                    .HandleAsync(new UpdatePullRequestTaskRequest(workspace, repo, id, taskId, null, "RESOLVED"), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, completeIdArg, completeTaskIdOption);
        tasksCommand.Subcommands.Add(completeCommand);

        var deleteCommand = new Command("delete", "Delete a PR task");
        var deleteIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var deleteTaskIdOption = new Option<int>("--task-id") { Description = "Task ID", Required = true };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        deleteCommand.Arguments.Add(deleteIdArg);
        deleteCommand.Options.Add(deleteTaskIdOption);
        deleteCommand.Options.Add(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string? repo, int id, int taskId, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete task #{taskId} on PR #{id}? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeletePullRequestTaskHandler>()
                    .HandleAsync(new DeletePullRequestTaskRequest(workspace, repo, id, taskId), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, deleteIdArg, deleteTaskIdOption, yesOption);
        tasksCommand.Subcommands.Add(deleteCommand);

        return tasksCommand;
    }

    // Resolve and reopen are the same resource under POST and DELETE, so one
    // builder makes both rather than two near-identical blocks.
    private static Command CreateResolveCommand(
        IServiceProvider services,
        Option<string?> workspaceOption,
        Option<string?> repoOption,
        string name,
        string description,
        bool resolve)
    {
        var command = new Command(name, description);
        var idArg = new Argument<int>("id") { Description = "Pull request ID" };
        var commentOption = new Option<int>("--comment-id") { Description = "Comment ID", Required = true };
        command.Arguments.Add(idArg);
        command.Options.Add(commentOption);
        command.SetHandler((string? workspace, string? repo, int id, int commentId) =>
            CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<ResolvePullRequestCommentHandler>()
                    .HandleAsync(new ResolvePullRequestCommentRequest(workspace, repo, id, commentId, resolve), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, idArg, commentOption);
        return command;
    }

}
