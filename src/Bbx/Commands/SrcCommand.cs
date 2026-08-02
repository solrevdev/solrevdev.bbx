using System.CommandLine;
using Bbx.Features.Source.CatSource;
using Bbx.Features.Source.LsSource;
using Bbx.Features.Source.WriteSource;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class SrcCommand
{
    public static Command Create(IServiceProvider services)
    {
        var workspaceOption = CommandOptions.CreateWorkspaceOption();
        var repoOption = CommandOptions.CreateRepoOption();
        var command = new Command("src", "Browse and write repository source files");
        command.AddGlobalOption(workspaceOption);
        command.AddGlobalOption(repoOption);

        var lsCommand = new Command("ls", "List entries at a path");
        var lsRefOption = new Option<string>("--ref", "Commit hash or branch name") { IsRequired = true };
        var lsPathArg = new Argument<string?>("path", () => null, "Directory path (defaults to repo root)");
        var lsLimitOption = new Option<int>("--limit", () => 100, "Maximum entries to list");
        lsCommand.AddOption(lsRefOption);
        lsCommand.AddArgument(lsPathArg);
        lsCommand.AddOption(lsLimitOption);
        lsCommand.SetHandler((string? workspace, string? repo, string @ref, string? path, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<LsSourceHandler>()
                    .HandleAsync(new LsSourceRequest(workspace, repo, @ref, path, limit), CancellationToken.None)),
            workspaceOption, repoOption, lsRefOption, lsPathArg, lsLimitOption);
        command.AddCommand(lsCommand);

        var catCommand = new Command("cat", "Print file contents at a ref");
        var catRefOption = new Option<string>("--ref", "Commit hash or branch name") { IsRequired = true };
        var catPathArg = new Argument<string>("path", "File path");
        catCommand.AddOption(catRefOption);
        catCommand.AddArgument(catPathArg);
        catCommand.SetHandler((string? workspace, string? repo, string @ref, string path) =>
            CommandRunner.RunRawAsync(() =>
                services.GetRequiredService<CatSourceHandler>()
                    .HandleAsync(new CatSourceRequest(workspace, repo, @ref, path), CancellationToken.None)),
            workspaceOption, repoOption, catRefOption, catPathArg);
        command.AddCommand(catCommand);

        var writeCommand = new Command("write", "Commit one or more files to a branch");
        var writeBranchOption = new Option<string>("--branch", "Target branch") { IsRequired = true };
        var writeMessageOption = new Option<string>("--message", "Commit message") { IsRequired = true };
        var writeFileOption = new Option<string[]>("--file",
            "File mapping: <local>=<repo-path>. Repeat for multiple files.")
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = false,
        };
        var writeAuthorOption = new Option<string?>("--author", "Author override (e.g., 'Jane Doe <jane@example.com>')");
        writeCommand.AddOption(writeBranchOption);
        writeCommand.AddOption(writeMessageOption);
        writeCommand.AddOption(writeFileOption);
        writeCommand.AddOption(writeAuthorOption);
        writeCommand.SetHandler((string? workspace, string? repo, string branch, string message, string[] files, string? author) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<WriteSourceHandler>()
                    .HandleAsync(new WriteSourceRequest(workspace, repo, branch, message, files, author), CancellationToken.None)),
            workspaceOption, repoOption, writeBranchOption, writeMessageOption, writeFileOption, writeAuthorOption);
        command.AddCommand(writeCommand);

        return command;
    }
}
