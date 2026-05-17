using System.CommandLine;
using Bbx.Features.PullRequests.AddPullRequestComment;
using Bbx.Features.PullRequests.ApprovePullRequest;
using Bbx.Features.PullRequests.CreatePullRequest;
using Bbx.Features.PullRequests.DeclinePullRequest;
using Bbx.Features.PullRequests.ListPullRequestComments;
using Bbx.Features.PullRequests.ListPullRequests;
using Bbx.Features.PullRequests.MergePullRequest;
using Bbx.Features.PullRequests.PullRequestActivity;
using Bbx.Features.PullRequests.PullRequestDiff;
using Bbx.Features.PullRequests.PullRequestStatuses;
using Bbx.Features.PullRequests.UnapprovePullRequest;
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
        command.AddGlobalOption(workspaceOption);
        command.AddGlobalOption(repoOption);

        var listCommand = new Command("list", "List pull requests");
        var stateOption = new Option<string?>("--state", "Filter by state (OPEN, MERGED, DECLINED, SUPERSEDED)");
        var authorOption = new Option<string?>("--author", "Filter by author account ID");
        var limitOption = new Option<int>("--limit", () => 25, "Maximum PRs to list");
        listCommand.AddOption(stateOption);
        listCommand.AddOption(authorOption);
        listCommand.AddOption(limitOption);
        listCommand.SetHandler((string? workspace, string? repo, string? state, string? author, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListPullRequestsHandler>()
                    .HandleAsync(new ListPullRequestsRequest(workspace, repo, state, author, limit), CancellationToken.None)),
            workspaceOption, repoOption, stateOption, authorOption, limitOption);
        command.AddCommand(listCommand);

        var viewCommand = new Command("view", "View pull request details");
        var idArg = new Argument<int>("id", "Pull request ID");
        viewCommand.AddArgument(idArg);
        viewCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewPullRequestHandler>()
                    .HandleAsync(new ViewPullRequestRequest(workspace, repo, id), CancellationToken.None)),
            workspaceOption, repoOption, idArg);
        command.AddCommand(viewCommand);

        var createCommand = new Command("create", "Create a new pull request");
        var titleOption = new Option<string>("--title", "Pull request title") { IsRequired = true };
        var sourceOption = new Option<string>("--source", "Source branch") { IsRequired = true };
        var destOption = new Option<string>("--dest", "Destination branch") { IsRequired = true };
        var bodyOption = new Option<string?>("--body", "Pull request description");
        var reviewersOption = new Option<string[]?>("--reviewers", "Reviewer account IDs (UUID format)");
        var closeSourceOption = new Option<bool>("--close-source-branch", "Close source branch after merge");
        createCommand.AddOption(titleOption);
        createCommand.AddOption(sourceOption);
        createCommand.AddOption(destOption);
        createCommand.AddOption(bodyOption);
        createCommand.AddOption(reviewersOption);
        createCommand.AddOption(closeSourceOption);
        createCommand.SetHandler((string? workspace, string? repo, string title, string source, string dest, string? body, string[]? reviewers, bool closeSource) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreatePullRequestHandler>()
                    .HandleAsync(new CreatePullRequestRequest(workspace, repo, title, source, dest, body, reviewers, closeSource), CancellationToken.None)),
            workspaceOption, repoOption, titleOption, sourceOption, destOption, bodyOption, reviewersOption, closeSourceOption);
        command.AddCommand(createCommand);

        var mergeCommand = new Command("merge", "Merge a pull request");
        var mergeIdArg = new Argument<int>("id", "Pull request ID");
        var strategyOption = new Option<string>("--strategy", () => "merge", "Merge strategy (merge, squash, fast_forward)");
        var messageOption = new Option<string?>("--message", "Merge commit message");
        var closeSourceMergeOption = new Option<bool>("--close-source-branch", "Close source branch after merge");
        mergeCommand.AddArgument(mergeIdArg);
        mergeCommand.AddOption(strategyOption);
        mergeCommand.AddOption(messageOption);
        mergeCommand.AddOption(closeSourceMergeOption);
        mergeCommand.SetHandler((string? workspace, string? repo, int id, string strategy, string? message, bool closeSource) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<MergePullRequestHandler>()
                    .HandleAsync(new MergePullRequestRequest(workspace, repo, id, strategy, message, closeSource), CancellationToken.None)),
            workspaceOption, repoOption, mergeIdArg, strategyOption, messageOption, closeSourceMergeOption);
        command.AddCommand(mergeCommand);

        var approveCommand = new Command("approve", "Approve a pull request");
        var approveIdArg = new Argument<int>("id", "Pull request ID");
        approveCommand.AddArgument(approveIdArg);
        approveCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<ApprovePullRequestHandler>()
                    .HandleAsync(new ApprovePullRequestRequest(workspace, repo, id), CancellationToken.None)),
            workspaceOption, repoOption, approveIdArg);
        command.AddCommand(approveCommand);

        var unapproveCommand = new Command("unapprove", "Remove approval from a pull request");
        var unapproveIdArg = new Argument<int>("id", "Pull request ID");
        unapproveCommand.AddArgument(unapproveIdArg);
        unapproveCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<UnapprovePullRequestHandler>()
                    .HandleAsync(new UnapprovePullRequestRequest(workspace, repo, id), CancellationToken.None)),
            workspaceOption, repoOption, unapproveIdArg);
        command.AddCommand(unapproveCommand);

        var declineCommand = new Command("decline", "Decline a pull request");
        var declineIdArg = new Argument<int>("id", "Pull request ID");
        var declineReasonOption = new Option<string?>("--reason", "Reason for declining");
        declineCommand.AddArgument(declineIdArg);
        declineCommand.AddOption(declineReasonOption);
        declineCommand.SetHandler((string? workspace, string? repo, int id, string? reason) =>
            CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeclinePullRequestHandler>()
                    .HandleAsync(new DeclinePullRequestRequest(workspace, repo, id, reason), CancellationToken.None)),
            workspaceOption, repoOption, declineIdArg, declineReasonOption);
        command.AddCommand(declineCommand);

        var commentsCommand = new Command("comments", "List pull request comments");
        var commentsIdArg = new Argument<int>("id", "Pull request ID");
        commentsCommand.AddArgument(commentsIdArg);
        commentsCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListPullRequestCommentsHandler>()
                    .HandleAsync(new ListPullRequestCommentsRequest(workspace, repo, id), CancellationToken.None)),
            workspaceOption, repoOption, commentsIdArg);
        command.AddCommand(commentsCommand);

        var commentCommand = new Command("comment", "Add a comment to a pull request");
        var commentIdArg = new Argument<int>("id", "Pull request ID");
        var commentBodyOption = new Option<string>("--body", "Comment text") { IsRequired = true };
        commentCommand.AddArgument(commentIdArg);
        commentCommand.AddOption(commentBodyOption);
        commentCommand.SetHandler((string? workspace, string? repo, int id, string commentBody) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddPullRequestCommentHandler>()
                    .HandleAsync(new AddPullRequestCommentRequest(workspace, repo, id, commentBody), CancellationToken.None)),
            workspaceOption, repoOption, commentIdArg, commentBodyOption);
        command.AddCommand(commentCommand);

        var diffCommand = new Command("diff", "Show pull request diff");
        var diffIdArg = new Argument<int>("id", "Pull request ID");
        diffCommand.AddArgument(diffIdArg);
        diffCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunRawAsync(() =>
                services.GetRequiredService<PullRequestDiffHandler>()
                    .HandleAsync(new PullRequestDiffRequest(workspace, repo, id), CancellationToken.None)),
            workspaceOption, repoOption, diffIdArg);
        command.AddCommand(diffCommand);

        var activityCommand = new Command("activity", "Show pull request activity log");
        var activityIdArg = new Argument<int>("id", "Pull request ID");
        activityCommand.AddArgument(activityIdArg);
        activityCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<PullRequestActivityHandler>()
                    .HandleAsync(new PullRequestActivityRequest(workspace, repo, id), CancellationToken.None)),
            workspaceOption, repoOption, activityIdArg);
        command.AddCommand(activityCommand);

        var statusesCommand = new Command("statuses", "Show pull request commit statuses");
        var statusesIdArg = new Argument<int>("id", "Pull request ID");
        statusesCommand.AddArgument(statusesIdArg);
        statusesCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<PullRequestStatusesHandler>()
                    .HandleAsync(new PullRequestStatusesRequest(workspace, repo, id), CancellationToken.None)),
            workspaceOption, repoOption, statusesIdArg);
        command.AddCommand(statusesCommand);

        // The "effective default reviewers" endpoint is repo-scoped (not
        // PR-scoped), so we don't take a PR id here. Surfacing it under `pr`
        // matches reviewer-workflow muscle memory while pointing at the
        // repo-level endpoint that actually returns the data.
        var defaultReviewersCommand = new Command("default-reviewers",
            "Show effective default reviewers for the repository (inherited from project + repo)");
        var drLimitOption = new Option<int>("--limit", () => 25, "Maximum reviewers to list");
        defaultReviewersCommand.AddOption(drLimitOption);
        defaultReviewersCommand.SetHandler((string? workspace, string? repo, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<EffectiveDefaultReviewersHandler>()
                    .HandleAsync(new EffectiveDefaultReviewersRequest(workspace, repo, limit), CancellationToken.None)),
            workspaceOption, repoOption, drLimitOption);
        command.AddCommand(defaultReviewersCommand);

        return command;
    }
}
