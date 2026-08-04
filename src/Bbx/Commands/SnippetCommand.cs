using System.CommandLine;
using Bbx.Features.Snippets.CreateSnippet;
using Bbx.Features.Snippets.DeleteSnippet;
using Bbx.Features.Snippets.ListSnippets;
using Bbx.Features.Snippets.SnippetComments;
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

        command.SetHandler((string snippetId, string? workspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewSnippetHandler>()
                    .HandleAsync(new ViewSnippetRequest(snippetId, workspace), CommandBinding.CancellationToken)),
            snippetIdArg, workspaceOption);
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

        command.SetHandler((string snippetId, string? title, string[]? files, bool? isPrivate, string? workspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateSnippetHandler>()
                    .HandleAsync(new UpdateSnippetRequest(snippetId, title, files, isPrivate, workspace), CommandBinding.CancellationToken)),
            snippetIdArg, titleOption, fileOption, privateOption, workspaceOption);
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

        command.SetHandler(async (string snippetId, string? workspace, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete snippet '{snippetId}'? [y/N]: "))
                return;
            await CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<DeleteSnippetHandler>()
                    .HandleAsync(new DeleteSnippetRequest(snippetId, workspace), CommandBinding.CancellationToken));
        }, snippetIdArg, workspaceOption, yesOption);
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

        command.SetHandler(async (string snippetId, string? fileName, string? workspace, bool raw) =>
        {
            await CommandRunner.RunDirectAsync(() =>
                services.GetRequiredService<SnippetFilesHandler>()
                    .HandleAsync(new SnippetFilesRequest(snippetId, fileName, workspace, raw), CommandBinding.CancellationToken));
        }, snippetIdArg, fileNameArg, workspaceOption, rawOption);
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

        command.Arguments.Add(snippetIdArg);
        command.Options.Add(workspaceOption);
        command.Options.Add(addOption);
        command.Options.Add(deleteOption);

        command.SetHandler((string snippetId, string? workspace, string? addContent, int? deleteId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<SnippetCommentsHandler>()
                    .HandleAsync(new SnippetCommentsRequest(snippetId, workspace, addContent, deleteId), CommandBinding.CancellationToken)),
            snippetIdArg, workspaceOption, addOption, deleteOption);
        return command;
    }
}
