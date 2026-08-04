using System.CommandLine;
using Bbx.Features.PullRequests.AddPullRequestComment;
using Bbx.Features.PullRequests.ApprovePullRequest;
using Bbx.Features.PullRequests.CreatePullRequest;
using Bbx.Features.PullRequests.DeclinePullRequest;
using Bbx.Features.PullRequests.ListPullRequestComments;
using Bbx.Features.PullRequests.ListPullRequestCommits;
using Bbx.Features.PullRequests.ListPullRequests;
using Bbx.Features.PullRequests.MergePullRequest;
using Bbx.Features.PullRequests.PullRequestActivity;
using Bbx.Features.PullRequests.PullRequestDiff;
using Bbx.Features.PullRequests.PullRequestPatch;
using Bbx.Features.PullRequests.PullRequestStatuses;
using Bbx.Features.PullRequests.RequestChanges;
using Bbx.Features.PullRequests.Tasks.AddPullRequestTask;
using Bbx.Features.PullRequests.Tasks.DeletePullRequestTask;
using Bbx.Features.PullRequests.Tasks.ListPullRequestTasks;
using Bbx.Features.PullRequests.Tasks.UpdatePullRequestTask;
using Bbx.Features.PullRequests.UnapprovePullRequest;
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
        var closeSourceOption = new Option<bool>("--close-source-branch") { Description = "Close source branch after merge" };
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

        var mergeCommand = new Command("merge", "Merge a pull request");
        var mergeIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        var strategyOption = new Option<string>("--strategy")
        {
            Description = "Merge strategy (merge_commit, squash, fast_forward). 'merge' is accepted for merge_commit.",
            DefaultValueFactory = _ => "merge_commit",
        };
        var messageOption = new Option<string?>("--message") { Description = "Merge commit message" };
        var closeSourceMergeOption = new Option<bool>("--close-source-branch") { Description = "Close source branch after merge" };
        // Merging writes to the destination branch and cannot be undone from
        // here, so it confirms like the other destructive verbs.
        var mergeYesOption = new Option<bool>("--yes") { Description = "Skip confirmation prompt" };
        mergeCommand.Arguments.Add(mergeIdArg);
        mergeCommand.Options.Add(strategyOption);
        mergeCommand.Options.Add(messageOption);
        mergeCommand.Options.Add(closeSourceMergeOption);
        mergeCommand.Options.Add(mergeYesOption);
        mergeCommand.SetHandler(async (string? workspace, string? repo, int id, string strategy, string? message, bool closeSource, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Merge PR #{id} using '{strategy}'? [y/N]: "))
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

        var declineCommand = new Command("decline", "Decline a pull request");
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

        var activityCommand = new Command("activity", "Show pull request activity log");
        var activityIdArg = new Argument<int>("id") { Description = "Pull request ID" };
        activityCommand.Arguments.Add(activityIdArg);
        activityCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<PullRequestActivityHandler>()
                    .HandleAsync(new PullRequestActivityRequest(workspace, repo, id), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, activityIdArg);
        command.Subcommands.Add(activityCommand);

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
}
