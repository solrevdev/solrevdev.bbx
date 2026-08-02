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
        command.AddRecursiveOption(workspaceOption);
        command.AddRecursiveOption(repoOption);

        var listCommand = new Command("list", "List issues");
        var stateOption = new Option<string?>("--state") { Description = "Filter by state (new, open, resolved, on hold, invalid, duplicate, wontfix, closed)" };
        var priorityOption = new Option<string?>("--priority") { Description = "Filter by priority (trivial, minor, major, critical, blocker)" };
        var assigneeOption = new Option<string?>("--assignee") { Description = "Filter by assignee account ID" };
        var limitOption = new Option<int>("--limit") { Description = "Maximum issues to list", DefaultValueFactory = _ => 25 };
        listCommand.Options.Add(stateOption);
        listCommand.Options.Add(priorityOption);
        listCommand.Options.Add(assigneeOption);
        listCommand.Options.Add(limitOption);
        listCommand.SetHandler((string? workspace, string? repo, string? state, string? priority, string? assignee, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListIssuesHandler>()
                    .HandleAsync(new ListIssuesRequest(workspace, repo, state, priority, assignee, limit), CancellationToken.None)),
            workspaceOption, repoOption, stateOption, priorityOption, assigneeOption, limitOption);
        command.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View issue details");
        var idArg = new Argument<int>("id") { Description = "Issue ID" };
        viewCommand.Arguments.Add(idArg);
        viewCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewIssueHandler>()
                    .HandleAsync(new ViewIssueRequest(workspace, repo, id), CancellationToken.None)),
            workspaceOption, repoOption, idArg);
        command.Subcommands.Add(viewCommand);

        var createCommand = new Command("create", "Create a new issue");
        var titleOption = new Option<string>("--title") { Description = "Issue title" , Required = true };
        var contentOption = new Option<string?>("--content") { Description = "Issue description" };
        var kindOption = new Option<string?>("--kind") { Description = "Issue kind (bug, enhancement, proposal, task)" };
        var priorityCreateOption = new Option<string?>("--priority") { Description = "Priority (trivial, minor, major, critical, blocker)" };
        createCommand.Options.Add(titleOption);
        createCommand.Options.Add(contentOption);
        createCommand.Options.Add(kindOption);
        createCommand.Options.Add(priorityCreateOption);
        createCommand.SetHandler((string? workspace, string? repo, string title, string? content, string? kind, string? priority) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateIssueHandler>()
                    .HandleAsync(new CreateIssueRequest(workspace, repo, title, content, kind, priority), CancellationToken.None)),
            workspaceOption, repoOption, titleOption, contentOption, kindOption, priorityCreateOption);
        command.Subcommands.Add(createCommand);

        var updateCommand = new Command("update", "Update an issue");
        var updateIdArg = new Argument<int>("id") { Description = "Issue ID" };
        var updateTitleOption = new Option<string?>("--title") { Description = "New title" };
        var updateStateOption = new Option<string?>("--state") { Description = "New state" };
        var updatePriorityOption = new Option<string?>("--priority") { Description = "New priority" };
        var updateAssigneeOption = new Option<string?>("--assignee") { Description = "Assignee account ID" };
        updateCommand.Arguments.Add(updateIdArg);
        updateCommand.Options.Add(updateTitleOption);
        updateCommand.Options.Add(updateStateOption);
        updateCommand.Options.Add(updatePriorityOption);
        updateCommand.Options.Add(updateAssigneeOption);
        updateCommand.SetHandler((string? workspace, string? repo, int id, string? title, string? state, string? priority, string? assignee) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateIssueHandler>()
                    .HandleAsync(new UpdateIssueRequest(workspace, repo, id, title, state, priority, assignee), CancellationToken.None)),
            workspaceOption, repoOption, updateIdArg, updateTitleOption, updateStateOption, updatePriorityOption, updateAssigneeOption);
        command.Subcommands.Add(updateCommand);

        var deleteCommand = new Command("delete", "Delete an issue");
        var deleteIdArg = new Argument<int>("id") { Description = "Issue ID" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        deleteCommand.Arguments.Add(deleteIdArg);
        deleteCommand.Options.Add(yesOption);
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
        command.Subcommands.Add(deleteCommand);

        var commentsCommand = new Command("comments", "List issue comments");
        var commentsIdArg = new Argument<int>("id") { Description = "Issue ID" };
        commentsCommand.Arguments.Add(commentsIdArg);
        commentsCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListIssueCommentsHandler>()
                    .HandleAsync(new ListIssueCommentsRequest(workspace, repo, id), CancellationToken.None)),
            workspaceOption, repoOption, commentsIdArg);
        command.Subcommands.Add(commentsCommand);

        var commentCommand = new Command("comment", "Add a comment to an issue");
        var commentIdArg = new Argument<int>("id") { Description = "Issue ID" };
        var commentBodyOption = new Option<string>("--body") { Description = "Comment text" , Required = true };
        commentCommand.Arguments.Add(commentIdArg);
        commentCommand.Options.Add(commentBodyOption);
        commentCommand.SetHandler((string? workspace, string? repo, int id, string commentBody) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddIssueCommentHandler>()
                    .HandleAsync(new AddIssueCommentRequest(workspace, repo, id, commentBody), CancellationToken.None)),
            workspaceOption, repoOption, commentIdArg, commentBodyOption);
        command.Subcommands.Add(commentCommand);

        return command;
    }
}
