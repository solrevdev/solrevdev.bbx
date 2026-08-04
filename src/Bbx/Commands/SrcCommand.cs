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
        command.AddRecursiveOption(workspaceOption);
        command.AddRecursiveOption(repoOption);

        var lsCommand = new Command("ls", "List entries at a path");
        var lsRefOption = new Option<string>("--ref") { Description = "Commit hash or branch name", Required = true };
        var lsPathArg = new Argument<string?>("path") { Description = "Directory path (defaults to repo root)", DefaultValueFactory = _ => null };
        var lsLimitOption = new Option<int>("--limit") { Description = "Maximum entries to list", DefaultValueFactory = _ => 100 };
        lsCommand.Options.Add(lsRefOption);
        lsCommand.Arguments.Add(lsPathArg);
        lsCommand.Options.Add(lsLimitOption);
        lsCommand.SetHandler((string? workspace, string? repo, string @ref, string? path, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<LsSourceHandler>()
                    .HandleAsync(new LsSourceRequest(workspace, repo, @ref, path, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, lsRefOption, lsPathArg, lsLimitOption);
        command.Subcommands.Add(lsCommand);

        var catCommand = new Command("cat", "Print file contents at a ref");
        var catRefOption = new Option<string>("--ref") { Description = "Commit hash or branch name", Required = true };
        var catPathArg = new Argument<string>("path") { Description = "File path" };
        catCommand.Options.Add(catRefOption);
        catCommand.Arguments.Add(catPathArg);
        catCommand.SetHandler((string? workspace, string? repo, string @ref, string path) =>
            CommandRunner.RunRawAsync(() =>
                services.GetRequiredService<CatSourceHandler>()
                    .HandleAsync(new CatSourceRequest(workspace, repo, @ref, path), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, catRefOption, catPathArg);
        command.Subcommands.Add(catCommand);

        var writeCommand = new Command("write", "Commit one or more files to a branch");
        var writeBranchOption = new Option<string>("--branch") { Description = "Target branch", Required = true };
        var writeMessageOption = new Option<string>("--message") { Description = "Commit message", Required = true };
        var writeFileOption = new Option<string[]>("--file")
        {
            Description = "File mapping: <local>=<repo-path>. Repeat for multiple files.",
            Required = true,
            AllowMultipleArgumentsPerToken = false,
        };
        var writeAuthorOption = new Option<string?>("--author") { Description = "Author override (e.g., 'Jane Doe <jane@example.com>')" };
        writeCommand.Options.Add(writeBranchOption);
        writeCommand.Options.Add(writeMessageOption);
        writeCommand.Options.Add(writeFileOption);
        writeCommand.Options.Add(writeAuthorOption);
        writeCommand.SetHandler((string? workspace, string? repo, string branch, string message, string[] files, string? author) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<WriteSourceHandler>()
                    .HandleAsync(new WriteSourceRequest(workspace, repo, branch, message, files, author), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, writeBranchOption, writeMessageOption, writeFileOption, writeAuthorOption);
        command.Subcommands.Add(writeCommand);

        return command;
    }
}
