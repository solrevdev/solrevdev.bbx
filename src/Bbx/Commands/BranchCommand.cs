using System.CommandLine;
using Bbx.Features.Branches.AddBranchRestriction;
using Bbx.Features.Branches.CreateBranch;
using Bbx.Features.Branches.DeleteBranch;
using Bbx.Features.Branches.DeleteBranchRestriction;
using Bbx.Features.Branches.ListBranches;
using Bbx.Features.Branches.ListBranchRestrictions;
using Bbx.Features.Branches.ViewBranch;
using Bbx.Features.Tags.CreateTag;
using Bbx.Features.Tags.DeleteTag;
using Bbx.Features.Tags.ListTags;
using Bbx.Features.Tags.ViewTag;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class BranchCommand
{
    public static Command Create(IServiceProvider services)
    {
        var workspaceOption = CommandOptions.CreateWorkspaceOption();
        var repoOption = CommandOptions.CreateRepoOption();
        var command = new Command("branch", "Manage branches");
        command.AddRecursiveOption(workspaceOption);
        command.AddRecursiveOption(repoOption);

        var listCommand = new Command("list", "List branches");
        var limitOption = new Option<int>("--limit") { Description = "Maximum branches to list", DefaultValueFactory = _ => 25 };
        var sortOption = new Option<string?>("--sort") { Description = "Sort by field (e.g., -name for descending)" };
        var queryOption = new Option<string?>("--query") { Description = "BBQL query filter" };
        listCommand.Options.Add(limitOption);
        listCommand.Options.Add(sortOption);
        listCommand.Options.Add(queryOption);
        listCommand.SetHandler((string? workspace, string? repo, int limit, string? sort, string? query) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListBranchesHandler>()
                    .HandleAsync(new ListBranchesRequest(workspace, repo, limit, sort, query), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, limitOption, sortOption, queryOption);
        command.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View branch details");
        var nameArg = new Argument<string>("name") { Description = "Branch name" };
        viewCommand.Arguments.Add(nameArg);
        viewCommand.SetHandler((string? workspace, string? repo, string name) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewBranchHandler>()
                    .HandleAsync(new ViewBranchRequest(workspace, repo, name), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, nameArg);
        command.Subcommands.Add(viewCommand);

        var createCommand = new Command("create", "Create a new branch");
        var createNameArg = new Argument<string>("name") { Description = "Branch name" };
        var targetOption = new Option<string>("--target") { Description = "Target commit hash or branch name", Required = true };
        createCommand.Arguments.Add(createNameArg);
        createCommand.Options.Add(targetOption);
        createCommand.SetHandler((string? workspace, string? repo, string name, string target) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateBranchHandler>()
                    .HandleAsync(new CreateBranchRequest(workspace, repo, name, target), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, createNameArg, targetOption);
        command.Subcommands.Add(createCommand);

        var deleteCommand = new Command("delete", "Delete a branch");
        var deleteNameArg = new Argument<string>("name") { Description = "Branch name to delete" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        deleteCommand.Arguments.Add(deleteNameArg);
        deleteCommand.Options.Add(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string? repo, string name, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete branch '{name}'? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteBranchHandler>()
                    .HandleAsync(new DeleteBranchRequest(workspace, repo, name), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, deleteNameArg, yesOption);
        command.Subcommands.Add(deleteCommand);

        var restrictionsCommand = new Command("restrictions", "Manage branch restrictions");

        var restrictionsListCommand = new Command("list", "List branch restrictions");
        restrictionsListCommand.SetHandler((string? workspace, string? repo) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListBranchRestrictionsHandler>()
                    .HandleAsync(new ListBranchRestrictionsRequest(workspace, repo), CommandBinding.CancellationToken)),
            workspaceOption, repoOption);
        restrictionsCommand.Subcommands.Add(restrictionsListCommand);

        var restrictionsAddCommand = new Command("add", "Add a branch restriction");
        var kindOption = new Option<string>("--kind") { Description = "Restriction kind (push, force, delete, require_passing_builds_to_merge, require_approvals_to_merge, etc.)", Required = true };
        var patternOption = new Option<string>("--pattern") { Description = "Branch pattern (glob or exact match)", Required = true };
        restrictionsAddCommand.Options.Add(kindOption);
        restrictionsAddCommand.Options.Add(patternOption);
        restrictionsAddCommand.SetHandler((string? workspace, string? repo, string kind, string pattern) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddBranchRestrictionHandler>()
                    .HandleAsync(new AddBranchRestrictionRequest(workspace, repo, kind, pattern), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, kindOption, patternOption);
        restrictionsCommand.Subcommands.Add(restrictionsAddCommand);

        var restrictionsDeleteCommand = new Command("delete", "Delete a branch restriction");
        var restrictionIdArg = new Argument<int>("id") { Description = "Restriction ID" };
        // Confirms like the other delete verbs; removing a restriction relaxes
        // protection on a branch.
        var restrictionYesOption = new Option<bool>("--yes") { Description = "Skip confirmation prompt" };
        restrictionsDeleteCommand.Arguments.Add(restrictionIdArg);
        restrictionsDeleteCommand.Options.Add(restrictionYesOption);
        restrictionsDeleteCommand.SetHandler(async (string? workspace, string? repo, int id, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete branch restriction #{id}? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteBranchRestrictionHandler>()
                    .HandleAsync(new DeleteBranchRestrictionRequest(workspace, repo, id), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, restrictionIdArg, restrictionYesOption);
        restrictionsCommand.Subcommands.Add(restrictionsDeleteCommand);

        command.Subcommands.Add(restrictionsCommand);

        command.Subcommands.Add(CreateTagCommand(services, workspaceOption, repoOption));

        return command;
    }

    private static Command CreateTagCommand(IServiceProvider services, Option<string?> workspaceOption, Option<string?> repoOption)
    {
        var tagCommand = new Command("tag", "Manage tags");

        var listCommand = new Command("list", "List tags");
        var listLimitOption = new Option<int>("--limit") { Description = "Maximum tags to list", DefaultValueFactory = _ => 25 };
        var listSortOption = new Option<string?>("--sort") { Description = "Sort by field (e.g., -name for descending)" };
        var listQueryOption = new Option<string?>("--query") { Description = "BBQL query filter" };
        listCommand.Options.Add(listLimitOption);
        listCommand.Options.Add(listSortOption);
        listCommand.Options.Add(listQueryOption);
        listCommand.SetHandler((string? workspace, string? repo, int limit, string? sort, string? query) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListTagsHandler>()
                    .HandleAsync(new ListTagsRequest(workspace, repo, limit, sort, query), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, listLimitOption, listSortOption, listQueryOption);
        tagCommand.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View tag details");
        var viewNameArg = new Argument<string>("name") { Description = "Tag name" };
        viewCommand.Arguments.Add(viewNameArg);
        viewCommand.SetHandler((string? workspace, string? repo, string name) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewTagHandler>()
                    .HandleAsync(new ViewTagRequest(workspace, repo, name), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, viewNameArg);
        tagCommand.Subcommands.Add(viewCommand);

        var createCommand = new Command("create", "Create a new tag");
        var createNameArg = new Argument<string>("name") { Description = "Tag name" };
        var createTargetOption = new Option<string>("--target") { Description = "Target commit hash or branch name", Required = true };
        var createMessageOption = new Option<string?>("--message") { Description = "Annotation message (creates an annotated tag)" };
        createCommand.Arguments.Add(createNameArg);
        createCommand.Options.Add(createTargetOption);
        createCommand.Options.Add(createMessageOption);
        createCommand.SetHandler((string? workspace, string? repo, string name, string target, string? message) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateTagHandler>()
                    .HandleAsync(new CreateTagRequest(workspace, repo, name, target, message), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, createNameArg, createTargetOption, createMessageOption);
        tagCommand.Subcommands.Add(createCommand);

        var deleteCommand = new Command("delete", "Delete a tag");
        var deleteNameArg = new Argument<string>("name") { Description = "Tag name to delete" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        deleteCommand.Arguments.Add(deleteNameArg);
        deleteCommand.Options.Add(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string? repo, string name, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete tag '{name}'? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteTagHandler>()
                    .HandleAsync(new DeleteTagRequest(workspace, repo, name), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, deleteNameArg, yesOption);
        tagCommand.Subcommands.Add(deleteCommand);

        return tagCommand;
    }
}
