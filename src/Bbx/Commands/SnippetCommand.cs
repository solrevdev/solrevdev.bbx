using System.CommandLine;
using Bbx.Features.Snippets.CreateSnippet;
using Bbx.Features.Snippets.DeleteSnippet;
using Bbx.Features.Snippets.ListSnippets;
using Bbx.Features.Snippets.SnippetComments;
using Bbx.Features.Snippets.SnippetCommits;
using Bbx.Features.Snippets.SnippetDiff;
using Bbx.Features.Snippets.SnippetFiles;
using Bbx.Features.Snippets.SnippetWatch;
using Bbx.Features.Snippets.UpdateSnippet;
using Bbx.Features.Snippets.ViewSnippet;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class SnippetCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("snippet", "Manage Bitbucket snippets");

        command.Subcommands.Add(CreateListCommand(services));
        command.Subcommands.Add(CreateViewCommand(services));
        command.Subcommands.Add(CreateCreateCommand(services));
        command.Subcommands.Add(CreateUpdateCommand(services));
        command.Subcommands.Add(CreateDeleteCommand(services));
        command.Subcommands.Add(CreateFilesCommand(services));
        command.Subcommands.Add(CreateWatchCommand(services));
        command.Subcommands.Add(CreateCommentsCommand(services));
        command.Subcommands.Add(CreateCommitsCommand(services));
        command.Subcommands.Add(CreateDiffCommand(services, "diff", "Show a snippet revision as a diff", asPatch: false));
        command.Subcommands.Add(CreateDiffCommand(services, "patch", "Show a snippet revision as a git-format patch", asPatch: true));

        return command;
    }

    private static Command CreateListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List snippets");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified, omit for personal snippets)" };
        var roleOption = new Option<string?>("--role", "-r") { Description = "Filter by role: owner, contributor, member" };
        var limitOption = new Option<int>("--limit", "-l") { Description = "Maximum number of snippets to return", DefaultValueFactory = _ => 25 };

        command.Options.Add(workspaceOption);
        command.Options.Add(roleOption);
        command.Options.Add(limitOption);

        command.SetHandler((string? workspace, string? role, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListSnippetsHandler>()
                    .HandleAsync(new ListSnippetsRequest(workspace, role, limit), CommandBinding.CancellationToken)),
            workspaceOption, roleOption, limitOption);
        return command;
    }

    private static Command CreateViewCommand(IServiceProvider services)
    {
        var command = new Command("view", "View a snippet");
        var snippetIdArg = new Argument<string>("snippet-id") { Description = "Snippet ID" };
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };

        command.Arguments.Add(snippetIdArg);
        command.Options.Add(workspaceOption);

        var revisionOption = new Option<string?>("--revision")
        { Description = "Pin to this revision rather than the latest" };
        command.Options.Add(revisionOption);
        command.SetHandler((string snippetId, string? workspace, string? revision) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewSnippetHandler>()
                    .HandleAsync(new ViewSnippetRequest(snippetId, workspace, revision), CommandBinding.CancellationToken)),
            snippetIdArg, workspaceOption, revisionOption);
        return command;
    }

    private static Command CreateCreateCommand(IServiceProvider services)
    {
        var command = new Command("create", "Create a new snippet");
        var titleOption = new Option<string>("--title", "-t") { Description = "Snippet title", Required = true };
        var fileOption = new Option<string[]>("--file", "-f") { Description = "File(s) to include in snippet (can be specified multiple times)", Required = true, AllowMultipleArgumentsPerToken = true };
        var privateOption = new Option<bool>("--private", "-p") { Description = "Make snippet private", DefaultValueFactory = _ => false };
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };

        command.Options.Add(titleOption);
        command.Options.Add(fileOption);
        command.Options.Add(privateOption);
        command.Options.Add(workspaceOption);

        command.SetHandler((string title, string[] files, bool isPrivate, string? workspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateSnippetHandler>()
                    .HandleAsync(new CreateSnippetRequest(title, files, isPrivate, workspace), CommandBinding.CancellationToken)),
            titleOption, fileOption, privateOption, workspaceOption);
        return command;
    }

    private static Command CreateUpdateCommand(IServiceProvider services)
    {
        var command = new Command("update", "Update an existing snippet");
        var snippetIdArg = new Argument<string>("snippet-id") { Description = "Snippet ID" };
        var titleOption = new Option<string?>("--title", "-t") { Description = "New title" };
        var fileOption = new Option<string[]?>("--file", "-f") { Description = "File(s) to update/add (can be specified multiple times)" };
        var privateOption = new Option<bool?>("--private", "-p") { Description = "Make snippet private" };
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };

        command.Arguments.Add(snippetIdArg);
        command.Options.Add(titleOption);
        command.Options.Add(fileOption);
        command.Options.Add(privateOption);
        command.Options.Add(workspaceOption);
        var revisionOption = new Option<string?>("--revision")
        { Description = "Pin to this revision. The write is refused if the snippet has moved on since." };
        command.Options.Add(revisionOption);

        command.SetHandler((string snippetId, string? title, string[]? files, bool? isPrivate, string? workspace, string? revision) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateSnippetHandler>()
                    .HandleAsync(new UpdateSnippetRequest(snippetId, title, files, isPrivate, workspace, revision), CommandBinding.CancellationToken)),
            snippetIdArg, titleOption, fileOption, privateOption, workspaceOption, revisionOption);
        return command;
    }

    private static Command CreateDeleteCommand(IServiceProvider services)
    {
        var command = new Command("delete", "Delete a snippet");
        var snippetIdArg = new Argument<string>("snippet-id") { Description = "Snippet ID" };
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };
        var yesOption = new Option<bool>("--yes", "-y") { Description = "Skip confirmation prompt", DefaultValueFactory = _ => false };

        command.Arguments.Add(snippetIdArg);
        command.Options.Add(workspaceOption);
        command.Options.Add(yesOption);
        var revisionOption = new Option<string?>("--revision")
        { Description = "Pin to this revision. The write is refused if the snippet has moved on since." };
        command.Options.Add(revisionOption);

        command.SetHandler(async (string snippetId, string? workspace, bool yes, string? revision) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete snippet '{snippetId}'? [y/N]: "))
                return;
            await CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<DeleteSnippetHandler>()
                    .HandleAsync(new DeleteSnippetRequest(snippetId, workspace, revision), CommandBinding.CancellationToken));
        }, snippetIdArg, workspaceOption, yesOption, revisionOption);
        return command;
    }

    private static Command CreateFilesCommand(IServiceProvider services)
    {
        var command = new Command("files", "List or get files in a snippet");
        var snippetIdArg = new Argument<string>("snippet-id") { Description = "Snippet ID" };
        var fileNameArg = new Argument<string?>("file-name") { Description = "File name to retrieve (optional)", DefaultValueFactory = _ => null };
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };
        var rawOption = new Option<bool>("--raw", "-r") { Description = "Output raw file content (only when file-name specified)", DefaultValueFactory = _ => false };

        command.Arguments.Add(snippetIdArg);
        command.Arguments.Add(fileNameArg);
        command.Options.Add(workspaceOption);
        command.Options.Add(rawOption);
        var revisionOption = new Option<string?>("--revision")
        { Description = "Read the file as it was at this revision" };
        command.Options.Add(revisionOption);

        command.SetHandler(async (string snippetId, string? fileName, string? workspace, bool raw, string? revision) =>
        {
            await CommandRunner.RunDirectAsync(() =>
                services.GetRequiredService<SnippetFilesHandler>()
                    .HandleAsync(new SnippetFilesRequest(snippetId, fileName, workspace, raw, revision), CommandBinding.CancellationToken));
        }, snippetIdArg, fileNameArg, workspaceOption, rawOption, revisionOption);
        return command;
    }

    private static Command CreateWatchCommand(IServiceProvider services)
    {
        var command = new Command("watch", "Watch/unwatch a snippet or list watchers");
        var snippetIdArg = new Argument<string>("snippet-id") { Description = "Snippet ID" };
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };
        var listOption = new Option<bool>("--list", "-l") { Description = "List watchers instead of watching", DefaultValueFactory = _ => false };
        var unwatchOption = new Option<bool>("--unwatch", "-u") { Description = "Stop watching the snippet", DefaultValueFactory = _ => false };

        command.Arguments.Add(snippetIdArg);
        command.Options.Add(workspaceOption);
        command.Options.Add(listOption);
        command.Options.Add(unwatchOption);

        command.SetHandler((string snippetId, string? workspace, bool list, bool unwatch) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<SnippetWatchHandler>()
                    .HandleAsync(new SnippetWatchRequest(snippetId, workspace, list, unwatch), CommandBinding.CancellationToken)),
            snippetIdArg, workspaceOption, listOption, unwatchOption);
        return command;
    }

    private static Command CreateCommentsCommand(IServiceProvider services)
    {
        var command = new Command("comments", "Manage snippet comments");
        var snippetIdArg = new Argument<string>("snippet-id") { Description = "Snippet ID" };
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };
        var addOption = new Option<string?>("--add", "-a") { Description = "Add a new comment with this content" };
        var deleteOption = new Option<int?>("--delete", "-d") { Description = "Delete comment by ID" };
        var updateOption = new Option<int?>("--update", "-u") { Description = "Edit comment by ID. Needs --content." };
        var contentOption = new Option<string?>("--content") { Description = "Replacement text for --update" };
        var viewOption = new Option<int?>("--view") { Description = "View a single comment by ID" };

        command.Arguments.Add(snippetIdArg);
        command.Options.Add(workspaceOption);
        command.Options.Add(addOption);
        command.Options.Add(deleteOption);
        command.Options.Add(updateOption);
        command.Options.Add(contentOption);
        command.Options.Add(viewOption);

        command.SetHandler((string snippetId, string? workspace, string? addContent, int? deleteId, int? updateId, string? content, int? viewId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<SnippetCommentsHandler>()
                    .HandleAsync(new SnippetCommentsRequest(snippetId, workspace, addContent, deleteId, updateId, content, viewId), CommandBinding.CancellationToken)),
            snippetIdArg, workspaceOption, addOption, deleteOption, updateOption, contentOption, viewOption);
        return command;
    }

    private static Command CreateCommitsCommand(IServiceProvider services)
    {
        var command = new Command("commits", "List a snippet's commits, or view one with --revision");
        var snippetIdArg = new Argument<string>("snippet-id") { Description = "Snippet ID" };
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };
        var revisionOption = new Option<string?>("--revision") { Description = "View this commit rather than the log" };
        var limitOption = new Option<int>("--limit") { Description = "Maximum commits to list", DefaultValueFactory = _ => 25 };

        command.Arguments.Add(snippetIdArg);
        command.Options.Add(workspaceOption);
        command.Options.Add(revisionOption);
        command.Options.Add(limitOption);

        command.SetHandler((string snippetId, string? workspace, string? revision, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<SnippetCommitsHandler>()
                    .HandleAsync(new SnippetCommitsRequest(snippetId, workspace, revision, limit), CommandBinding.CancellationToken)),
            snippetIdArg, workspaceOption, revisionOption, limitOption);
        return command;
    }

    // Diff and patch are the same resource in two formats, so one builder makes
    // both, the way `pr diff` and `pr patch` differ only in the trailing
    // segment.
    private static Command CreateDiffCommand(IServiceProvider services, string name, string description, bool asPatch)
    {
        var command = new Command(name, description);
        var snippetIdArg = new Argument<string>("snippet-id") { Description = "Snippet ID" };
        var revisionArg = new Argument<string>("revision") { Description = "Revision to render" };
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug (uses default if not specified)" };

        command.Arguments.Add(snippetIdArg);
        command.Arguments.Add(revisionArg);
        command.Options.Add(workspaceOption);

        command.SetHandler((string snippetId, string revision, string? workspace) =>
            CommandRunner.RunRawAsync(() =>
                services.GetRequiredService<SnippetDiffHandler>()
                    .HandleAsync(new SnippetDiffRequest(snippetId, workspace, revision, asPatch), CommandBinding.CancellationToken)),
            snippetIdArg, revisionArg, workspaceOption);
        return command;
    }

}
