using System.CommandLine;
using Bbx.Features.Branches.AddBranchRestriction;
using Bbx.Features.Branches.CreateBranch;
using Bbx.Features.Branches.DeleteBranch;
using Bbx.Features.Branches.DeleteBranchRestriction;
using Bbx.Features.Branches.ListBranchRestrictions;
using Bbx.Features.Branches.ListBranches;
using Bbx.Features.Branches.ViewBranch;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class BranchCommand
{
    public static Command Create(IServiceProvider services)
    {
        var workspaceOption = CommandOptions.CreateWorkspaceOption();
        var repoOption = CommandOptions.CreateRepoOption();
        var command = new Command("branch", "Manage branches");
        command.AddGlobalOption(workspaceOption);
        command.AddGlobalOption(repoOption);

        var listCommand = new Command("list", "List branches");
        var limitOption = new Option<int>("--limit", () => 25, "Maximum branches to list");
        var sortOption = new Option<string?>("--sort", "Sort by field (e.g., -name for descending)");
        var queryOption = new Option<string?>("--query", "BBQL query filter");
        listCommand.AddOption(limitOption);
        listCommand.AddOption(sortOption);
        listCommand.AddOption(queryOption);
        listCommand.SetHandler((string? workspace, string? repo, int limit, string? sort, string? query) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListBranchesHandler>()
                    .HandleAsync(new ListBranchesRequest(workspace, repo, limit, sort, query), CancellationToken.None)),
            workspaceOption, repoOption, limitOption, sortOption, queryOption);
        command.AddCommand(listCommand);

        var viewCommand = new Command("view", "View branch details");
        var nameArg = new Argument<string>("name", "Branch name");
        viewCommand.AddArgument(nameArg);
        viewCommand.SetHandler((string? workspace, string? repo, string name) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewBranchHandler>()
                    .HandleAsync(new ViewBranchRequest(workspace, repo, name), CancellationToken.None)),
            workspaceOption, repoOption, nameArg);
        command.AddCommand(viewCommand);

        var createCommand = new Command("create", "Create a new branch");
        var createNameArg = new Argument<string>("name", "Branch name");
        var targetOption = new Option<string>("--target", "Target commit hash or branch name") { IsRequired = true };
        createCommand.AddArgument(createNameArg);
        createCommand.AddOption(targetOption);
        createCommand.SetHandler((string? workspace, string? repo, string name, string target) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateBranchHandler>()
                    .HandleAsync(new CreateBranchRequest(workspace, repo, name, target), CancellationToken.None)),
            workspaceOption, repoOption, createNameArg, targetOption);
        command.AddCommand(createCommand);

        var deleteCommand = new Command("delete", "Delete a branch");
        var deleteNameArg = new Argument<string>("name", "Branch name to delete");
        var yesOption = new Option<bool>("--yes", "Skip confirmation");
        deleteCommand.AddArgument(deleteNameArg);
        deleteCommand.AddOption(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string? repo, string name, bool yes) =>
        {
            if (!yes)
            {
                Console.Write($"Delete branch '{name}'? (y/N): ");
                if (Console.ReadLine()?.Trim().ToLower() != "y")
                {
                    Console.WriteLine("Cancelled.");
                    return;
                }
            }
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteBranchHandler>()
                    .HandleAsync(new DeleteBranchRequest(workspace, repo, name), CancellationToken.None));
        }, workspaceOption, repoOption, deleteNameArg, yesOption);
        command.AddCommand(deleteCommand);

        var restrictionsCommand = new Command("restrictions", "Manage branch restrictions");

        var restrictionsListCommand = new Command("list", "List branch restrictions");
        restrictionsListCommand.SetHandler((string? workspace, string? repo) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListBranchRestrictionsHandler>()
                    .HandleAsync(new ListBranchRestrictionsRequest(workspace, repo), CancellationToken.None)),
            workspaceOption, repoOption);
        restrictionsCommand.AddCommand(restrictionsListCommand);

        var restrictionsAddCommand = new Command("add", "Add a branch restriction");
        var kindOption = new Option<string>("--kind", "Restriction kind (push, force, delete, require_passing_builds_to_merge, require_approvals_to_merge, etc.)") { IsRequired = true };
        var patternOption = new Option<string>("--pattern", "Branch pattern (glob or exact match)") { IsRequired = true };
        restrictionsAddCommand.AddOption(kindOption);
        restrictionsAddCommand.AddOption(patternOption);
        restrictionsAddCommand.SetHandler((string? workspace, string? repo, string kind, string pattern) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddBranchRestrictionHandler>()
                    .HandleAsync(new AddBranchRestrictionRequest(workspace, repo, kind, pattern), CancellationToken.None)),
            workspaceOption, repoOption, kindOption, patternOption);
        restrictionsCommand.AddCommand(restrictionsAddCommand);

        var restrictionsDeleteCommand = new Command("delete", "Delete a branch restriction");
        var restrictionIdArg = new Argument<int>("id", "Restriction ID");
        restrictionsDeleteCommand.AddArgument(restrictionIdArg);
        restrictionsDeleteCommand.SetHandler((string? workspace, string? repo, int id) =>
            CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteBranchRestrictionHandler>()
                    .HandleAsync(new DeleteBranchRestrictionRequest(workspace, repo, id), CancellationToken.None)),
            workspaceOption, repoOption, restrictionIdArg);
        restrictionsCommand.AddCommand(restrictionsDeleteCommand);

        command.AddCommand(restrictionsCommand);

        return command;
    }
}
