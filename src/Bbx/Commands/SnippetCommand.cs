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

        command.AddCommand(CreateListCommand(services));
        command.AddCommand(CreateViewCommand(services));
        command.AddCommand(CreateCreateCommand(services));
        command.AddCommand(CreateUpdateCommand(services));
        command.AddCommand(CreateDeleteCommand(services));
        command.AddCommand(CreateFilesCommand(services));
        command.AddCommand(CreateWatchCommand(services));
        command.AddCommand(CreateCommentsCommand(services));

        return command;
    }

    private static Command CreateListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List snippets");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (uses default if not specified, omit for personal snippets)");
        var roleOption = new Option<string?>(["--role", "-r"], "Filter by role: owner, contributor, member");
        var limitOption = new Option<int>(["--limit", "-l"], () => 25, "Maximum number of snippets to return");

        command.AddOption(workspaceOption);
        command.AddOption(roleOption);
        command.AddOption(limitOption);

        command.SetHandler((string? workspace, string? role, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListSnippetsHandler>()
                    .HandleAsync(new ListSnippetsRequest(workspace, role, limit), CancellationToken.None)),
            workspaceOption, roleOption, limitOption);
        return command;
    }

    private static Command CreateViewCommand(IServiceProvider services)
    {
        var command = new Command("view", "View a snippet");
        var snippetIdArg = new Argument<string>("snippet-id", "Snippet ID");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (uses default if not specified)");

        command.AddArgument(snippetIdArg);
        command.AddOption(workspaceOption);

        command.SetHandler((string snippetId, string? workspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewSnippetHandler>()
                    .HandleAsync(new ViewSnippetRequest(snippetId, workspace), CancellationToken.None)),
            snippetIdArg, workspaceOption);
        return command;
    }

    private static Command CreateCreateCommand(IServiceProvider services)
    {
        var command = new Command("create", "Create a new snippet");
        var titleOption = new Option<string>(["--title", "-t"], "Snippet title") { IsRequired = true };
        var fileOption = new Option<string[]>(["--file", "-f"], "File(s) to include in snippet (can be specified multiple times)") { IsRequired = true, AllowMultipleArgumentsPerToken = true };
        var privateOption = new Option<bool>(["--private", "-p"], () => false, "Make snippet private");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (uses default if not specified)");

        command.AddOption(titleOption);
        command.AddOption(fileOption);
        command.AddOption(privateOption);
        command.AddOption(workspaceOption);

        command.SetHandler((string title, string[] files, bool isPrivate, string? workspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateSnippetHandler>()
                    .HandleAsync(new CreateSnippetRequest(title, files, isPrivate, workspace), CancellationToken.None)),
            titleOption, fileOption, privateOption, workspaceOption);
        return command;
    }

    private static Command CreateUpdateCommand(IServiceProvider services)
    {
        var command = new Command("update", "Update an existing snippet");
        var snippetIdArg = new Argument<string>("snippet-id", "Snippet ID");
        var titleOption = new Option<string?>(["--title", "-t"], "New title");
        var fileOption = new Option<string[]?>(["--file", "-f"], "File(s) to update/add (can be specified multiple times)");
        var privateOption = new Option<bool?>(["--private", "-p"], "Make snippet private");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (uses default if not specified)");

        command.AddArgument(snippetIdArg);
        command.AddOption(titleOption);
        command.AddOption(fileOption);
        command.AddOption(privateOption);
        command.AddOption(workspaceOption);

        command.SetHandler((string snippetId, string? title, string[]? files, bool? isPrivate, string? workspace) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdateSnippetHandler>()
                    .HandleAsync(new UpdateSnippetRequest(snippetId, title, files, isPrivate, workspace), CancellationToken.None)),
            snippetIdArg, titleOption, fileOption, privateOption, workspaceOption);
        return command;
    }

    private static Command CreateDeleteCommand(IServiceProvider services)
    {
        var command = new Command("delete", "Delete a snippet");
        var snippetIdArg = new Argument<string>("snippet-id", "Snippet ID");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (uses default if not specified)");
        var yesOption = new Option<bool>(["--yes", "-y"], () => false, "Skip confirmation prompt");

        command.AddArgument(snippetIdArg);
        command.AddOption(workspaceOption);
        command.AddOption(yesOption);

        command.SetHandler(async (string snippetId, string? workspace, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete snippet '{snippetId}'? [y/N]: "))
                return;
            await CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<DeleteSnippetHandler>()
                    .HandleAsync(new DeleteSnippetRequest(snippetId, workspace), CancellationToken.None));
        }, snippetIdArg, workspaceOption, yesOption);
        return command;
    }

    private static Command CreateFilesCommand(IServiceProvider services)
    {
        var command = new Command("files", "List or get files in a snippet");
        var snippetIdArg = new Argument<string>("snippet-id", "Snippet ID");
        var fileNameArg = new Argument<string?>("file-name", () => null, "File name to retrieve (optional)");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (uses default if not specified)");
        var rawOption = new Option<bool>(["--raw", "-r"], () => false, "Output raw file content (only when file-name specified)");

        command.AddArgument(snippetIdArg);
        command.AddArgument(fileNameArg);
        command.AddOption(workspaceOption);
        command.AddOption(rawOption);

        command.SetHandler(async (string snippetId, string? fileName, string? workspace, bool raw) =>
        {
            try
            {
                await services.GetRequiredService<SnippetFilesHandler>()
                    .HandleAsync(new SnippetFilesRequest(snippetId, fileName, workspace, raw), CancellationToken.None);
            }
            catch (BbxUserException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Environment.ExitCode = 1;
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, snippetIdArg, fileNameArg, workspaceOption, rawOption);
        return command;
    }

    private static Command CreateWatchCommand(IServiceProvider services)
    {
        var command = new Command("watch", "Watch/unwatch a snippet or list watchers");
        var snippetIdArg = new Argument<string>("snippet-id", "Snippet ID");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (uses default if not specified)");
        var listOption = new Option<bool>(["--list", "-l"], () => false, "List watchers instead of watching");
        var unwatchOption = new Option<bool>(["--unwatch", "-u"], () => false, "Stop watching the snippet");

        command.AddArgument(snippetIdArg);
        command.AddOption(workspaceOption);
        command.AddOption(listOption);
        command.AddOption(unwatchOption);

        command.SetHandler((string snippetId, string? workspace, bool list, bool unwatch) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<SnippetWatchHandler>()
                    .HandleAsync(new SnippetWatchRequest(snippetId, workspace, list, unwatch), CancellationToken.None)),
            snippetIdArg, workspaceOption, listOption, unwatchOption);
        return command;
    }

    private static Command CreateCommentsCommand(IServiceProvider services)
    {
        var command = new Command("comments", "Manage snippet comments");
        var snippetIdArg = new Argument<string>("snippet-id", "Snippet ID");
        var workspaceOption = new Option<string?>(["--workspace", "-w"], "Workspace slug (uses default if not specified)");
        var addOption = new Option<string?>(["--add", "-a"], "Add a new comment with this content");
        var deleteOption = new Option<int?>(["--delete", "-d"], "Delete comment by ID");

        command.AddArgument(snippetIdArg);
        command.AddOption(workspaceOption);
        command.AddOption(addOption);
        command.AddOption(deleteOption);

        command.SetHandler((string snippetId, string? workspace, string? addContent, int? deleteId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<SnippetCommentsHandler>()
                    .HandleAsync(new SnippetCommentsRequest(snippetId, workspace, addContent, deleteId), CancellationToken.None)),
            snippetIdArg, workspaceOption, addOption, deleteOption);
        return command;
    }
}
