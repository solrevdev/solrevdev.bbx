using System.CommandLine;
using Bbx.Features.Downloads.DeleteDownload;
using Bbx.Features.Downloads.GetDownload;
using Bbx.Features.Downloads.ListDownloads;
using Bbx.Features.Downloads.UploadDownload;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class DownloadCommand
{
    public static Command Create(IServiceProvider services)
    {
        var workspaceOption = CommandOptions.CreateWorkspaceOption();
        var repoOption = CommandOptions.CreateRepoOption();
        var command = new Command("download", "Manage repository download artifacts");
        command.AddGlobalOption(workspaceOption);
        command.AddGlobalOption(repoOption);

        var listCommand = new Command("list", "List downloads");
        var listLimitOption = new Option<int>("--limit", () => 25, "Maximum downloads to list");
        listCommand.AddOption(listLimitOption);
        listCommand.SetHandler((string? workspace, string? repo, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListDownloadsHandler>()
                    .HandleAsync(new ListDownloadsRequest(workspace, repo, limit), CancellationToken.None)),
            workspaceOption, repoOption, listLimitOption);
        command.AddCommand(listCommand);

        var uploadCommand = new Command("upload", "Upload a new download artifact");
        var uploadFileOption = new Option<string>("--file", "Path to the local file to upload") { IsRequired = true };
        var uploadNameOption = new Option<string?>("--name", "Name on Bitbucket (defaults to local filename)");
        uploadCommand.AddOption(uploadFileOption);
        uploadCommand.AddOption(uploadNameOption);
        uploadCommand.SetHandler((string? workspace, string? repo, string filePath, string? name) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UploadDownloadHandler>()
                    .HandleAsync(new UploadDownloadRequest(workspace, repo, filePath, name), CancellationToken.None)),
            workspaceOption, repoOption, uploadFileOption, uploadNameOption);
        command.AddCommand(uploadCommand);

        var getCommand = new Command("get", "Download an artifact");
        var getFilenameArg = new Argument<string>("filename", "Artifact filename on Bitbucket");
        var getOutputOption = new Option<string?>("--output",
            "Write to <path> instead of stdout (and emit JSON metadata)");
        getCommand.AddArgument(getFilenameArg);
        getCommand.AddOption(getOutputOption);
        getCommand.SetHandler(async (string? workspace, string? repo, string filename, string? output) =>
        {
            if (!string.IsNullOrEmpty(output))
            {
                await CommandRunner.RunJsonAsync(async () =>
                {
                    var bytes = await services.GetRequiredService<GetDownloadHandler>()
                        .HandleAsync(new GetDownloadRequest(workspace, repo, filename, output), CancellationToken.None);
                    var dir = Path.GetDirectoryName(output);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    await File.WriteAllBytesAsync(output, bytes, CancellationToken.None);
                    return new
                    {
                        filename,
                        output,
                        size = bytes.LongLength,
                    };
                });
                return;
            }

            await CommandRunner.RunBinaryAsync(() =>
                services.GetRequiredService<GetDownloadHandler>()
                    .HandleAsync(new GetDownloadRequest(workspace, repo, filename, null), CancellationToken.None));
        }, workspaceOption, repoOption, getFilenameArg, getOutputOption);
        command.AddCommand(getCommand);

        var deleteCommand = new Command("delete", "Delete a download artifact");
        var deleteFilenameArg = new Argument<string>("filename", "Artifact filename");
        var yesOption = new Option<bool>("--yes", "Skip confirmation");
        deleteCommand.AddArgument(deleteFilenameArg);
        deleteCommand.AddOption(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string? repo, string filename, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete download '{filename}'? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteDownloadHandler>()
                    .HandleAsync(new DeleteDownloadRequest(workspace, repo, filename), CancellationToken.None));
        }, workspaceOption, repoOption, deleteFilenameArg, yesOption);
        command.AddCommand(deleteCommand);

        return command;
    }
}
