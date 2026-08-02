using System.CommandLine;
using Bbx.Features.Issues.AddIssueComment;
using Bbx.Features.Issues.CreateIssue;
using Bbx.Features.Issues.DeleteIssue;
using Bbx.Features.Issues.ListIssueComments;
using Bbx.Features.Issues.ListIssues;
using Bbx.Features.Issues.UpdateIssue;
using Bbx.Features.Issues.ViewIssue;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class IssueCommand
{
    public static Command Create(IServiceProvider services)
    {
        var workspaceOption = CommandOptions.CreateWorkspaceOption();
        var repoOption = CommandOptions.CreateRepoOption();
        var command = new Command("issue", "Manage issues");
        command.AddGlobalOption(workspaceOption);
        command.AddGlobalOption(repoOption);

        var listCommand = new Command("list", "List issues");
        var stateOption = new Option<string?>("--state", "Filter by state (new, open, resolved, on hold, invalid, duplicate, wontfix, closed)");
        var priorityOption = new Option<string?>("--priority", "Filter by priority (trivial, minor, major, critical, blocker)");
        var assigneeOption = new Option<string?>("--assignee", "Filter by assignee account ID");
        var limitOption = new Option<int>("--limit", () => 25, "Maximum issues to list");
        listCommand.AddOption(stateOption);
        listCommand.AddOption(priorityOption);
        listCommand.AddOption(assigneeOption);
        listCommand.AddOption(limitOption);
        listCommand.SetHandler((string? workspace, string? repo, string? state, string? priority, string? assignee, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListIssuesHandler>()
                    .HandleAsync(new ListIssuesRequest(workspace, repo, state, priority, assignee, limit), CancellationToken.None)),
            workspaceOption, repoOption, stateOption, priorityOption, assigneeOption, limitOption);
        command.AddCommand(listCommand);

        var viewCommand = new Command("view", "View issue details");
        var idArg = new Argument<int>("id", "Issue ID");
        viewCommand.AddArgument(idArg);
        viewCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewIssueHandler>()
                    .HandleAsync(new ViewIssueRequest(workspace, repo, id), CancellationToken.None)),
            workspaceOption, repoOption, idArg);
        command.AddCommand(viewCommand);

        var createCommand = new Command("create", "Create a new issue");
        var titleOption = new Option<string>("--title", "Issue title") { IsRequired = true };
        var contentOption = new Option<string?>("--content", "Issue description");
        var kindOption = new Option<string?>("--kind", "Issue kind (bug, enhancement, proposal, task)");
        var priorityCreateOption = new Option<string?>("--priority", "Priority (trivial, minor, major, critical, blocker)");
        createCommand.AddOption(titleOption);
        createCommand.AddOption(contentOption);
        createCommand.AddOption(kindOption);
        createCommand.AddOption(priorityCreateOption);
        createCommand.SetHandler((string? workspace, string? repo, string title, string? content, string? kind, string? priority) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateIssueHandler>()
                    .HandleAsync(new CreateIssueRequest(workspace, repo, title, content, kind, priority), CancellationToken.None)),
            workspaceOption, repoOption, titleOption, contentOption, kindOption, priorityCreateOption);
        command.AddCommand(createCommand);

        var updateCommand = new Command("update", "Update an issue");
        var updateIdArg = new Argument<int>("id", "Issue ID");
        var updateTitleOption = new Option<string?>("--title", "New title");
        var updateStateOption = new Option<string?>("--state", "New state");
        var updatePriorityOption = new Option<string?>("--priority", "New priority");
        var updateAssigneeOption = new Option<string?>("--assignee", "Assignee account ID");
        updateCommand.AddArgument(updateIdArg);
        updateCommand.AddOption(updateTitleOption);
        updateCommand.AddOption(updateStateOption);
        updateCommand.AddOption(updatePriorityOption);
        updateCommand.AddOption(updateAssigneeOption);
        updateCommand.SetHandler((string? workspace, string? repo, int id, string? title, string? state, string? priority, string? assignee) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateIssueHandler>()
                    .HandleAsync(new UpdateIssueRequest(workspace, repo, id, title, state, priority, assignee), CancellationToken.None)),
            workspaceOption, repoOption, updateIdArg, updateTitleOption, updateStateOption, updatePriorityOption, updateAssigneeOption);
        command.AddCommand(updateCommand);

        var deleteCommand = new Command("delete", "Delete an issue");
        var deleteIdArg = new Argument<int>("id", "Issue ID");
        var yesOption = new Option<bool>("--yes", "Skip confirmation");
        deleteCommand.AddArgument(deleteIdArg);
        deleteCommand.AddOption(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string? repo, int id, bool yes) =>
        {
            if (!yes)
            {
                Console.Write($"Delete issue #{id}? (y/N): ");
                if (Console.ReadLine()?.Trim().ToLower() != "y")
                {
                    Console.WriteLine("Cancelled.");
                    return;
                }
            }
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteIssueHandler>()
                    .HandleAsync(new DeleteIssueRequest(workspace, repo, id), CancellationToken.None));
        }, workspaceOption, repoOption, deleteIdArg, yesOption);
        command.AddCommand(deleteCommand);

        var commentsCommand = new Command("comments", "List issue comments");
        var commentsIdArg = new Argument<int>("id", "Issue ID");
        commentsCommand.AddArgument(commentsIdArg);
        commentsCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListIssueCommentsHandler>()
                    .HandleAsync(new ListIssueCommentsRequest(workspace, repo, id), CancellationToken.None)),
            workspaceOption, repoOption, commentsIdArg);
        command.AddCommand(commentsCommand);

        var commentCommand = new Command("comment", "Add a comment to an issue");
        var commentIdArg = new Argument<int>("id", "Issue ID");
        var commentBodyOption = new Option<string>("--body", "Comment text") { IsRequired = true };
        commentCommand.AddArgument(commentIdArg);
        commentCommand.AddOption(commentBodyOption);
        commentCommand.SetHandler((string? workspace, string? repo, int id, string commentBody) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddIssueCommentHandler>()
                    .HandleAsync(new AddIssueCommentRequest(workspace, repo, id, commentBody), CancellationToken.None)),
            workspaceOption, repoOption, commentIdArg, commentBodyOption);
        command.AddCommand(commentCommand);

        return command;
    }
}
