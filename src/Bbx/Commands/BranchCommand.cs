using System.CommandLine;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Commands;

public static class BranchCommand
{
    public static Command Create(Option<string?> workspaceOption, Option<string?> repoOption)
    {
        var command = new Command("branch", "Manage branches");
        command.AddOption(workspaceOption);
        command.AddOption(repoOption);

        // bbx branch list
        var listCommand = new Command("list", "List branches");
        var limitOption = new Option<int>("--limit", () => 25, "Maximum branches to list");
        var sortOption = new Option<string?>("--sort", "Sort by field (e.g., -name for descending)");
        var queryOption = new Option<string?>("--query", "BBQL query filter");
        listCommand.AddOption(limitOption);
        listCommand.AddOption(sortOption);
        listCommand.AddOption(queryOption);
        listCommand.SetHandler(async (string? workspace, string? repo, int limit, string? sort, string? query) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            var endpoint = $"/repositories/{workspace}/{repo}/refs/branches";
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(sort)) queryParams.Add($"sort={sort}");
            if (!string.IsNullOrEmpty(query)) queryParams.Add($"q={Uri.EscapeDataString(query)}");
            if (queryParams.Count > 0) endpoint += "?" + string.Join("&", queryParams);

            var count = 0;
            var branches = new List<object>();

            await foreach (var branch in client.GetPaginatedAsync<JsonElement>(endpoint))
            {
                branches.Add(new
                {
                    name = branch.TryGetProperty("name", out var n) ? n.GetString() : null,
                    target = branch.TryGetProperty("target", out var t) && t.TryGetProperty("hash", out var h) ? h.GetString()?[..12] : null,
                    author = branch.TryGetProperty("target", out var t2) && t2.TryGetProperty("author", out var a) && a.TryGetProperty("user", out var u) && u.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
                    date = branch.TryGetProperty("target", out var t3) && t3.TryGetProperty("date", out var d) ? d.GetString() : null
                });
                if (++count >= limit) break;
            }

            var output = new { workspace, repository = repo, count = branches.Count, branches };
            Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
        }, workspaceOption, repoOption, limitOption, sortOption, queryOption);
        command.AddCommand(listCommand);

        // bbx branch view
        var viewCommand = new Command("view", "View branch details");
        var nameArg = new Argument<string>("name", "Branch name");
        viewCommand.AddArgument(nameArg);
        viewCommand.SetHandler(async (string? workspace, string? repo, string name) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            try
            {
                var branch = await client.GetAsync<JsonElement>($"/repositories/{workspace}/{repo}/refs/branches/{Uri.EscapeDataString(name)}");
                Console.WriteLine(JsonSerializer.Serialize(branch, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, nameArg);
        command.AddCommand(viewCommand);

        // bbx branch create
        var createCommand = new Command("create", "Create a new branch");
        var createNameArg = new Argument<string>("name", "Branch name");
        var targetOption = new Option<string>("--target", "Target commit hash or branch name") { IsRequired = true };
        createCommand.AddArgument(createNameArg);
        createCommand.AddOption(targetOption);
        createCommand.SetHandler(async (string? workspace, string? repo, string name, string target) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            var body = new
            {
                name,
                target = new { hash = target }
            };

            try
            {
                var result = await client.PostAsync<JsonElement>($"/repositories/{workspace}/{repo}/refs/branches", body);
                Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, createNameArg, targetOption);
        command.AddCommand(createCommand);

        // bbx branch delete
        var deleteCommand = new Command("delete", "Delete a branch");
        var deleteNameArg = new Argument<string>("name", "Branch name to delete");
        var yesOption = new Option<bool>("--yes", "Skip confirmation");
        deleteCommand.AddArgument(deleteNameArg);
        deleteCommand.AddOption(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string? repo, string name, bool yes) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            if (!yes)
            {
                Console.Write($"Delete branch '{name}'? (y/N): ");
                if (Console.ReadLine()?.Trim().ToLower() != "y")
                {
                    Console.WriteLine("Cancelled.");
                    return;
                }
            }

            using var client = CreateClient(config);
            try
            {
                await client.DeleteAsync($"/repositories/{workspace}/{repo}/refs/branches/{Uri.EscapeDataString(name)}");
                Console.WriteLine($"✓ Deleted branch '{name}'");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, deleteNameArg, yesOption);
        command.AddCommand(deleteCommand);

        // bbx branch restrictions
        var restrictionsCommand = new Command("restrictions", "Manage branch restrictions");

        // bbx branch restrictions list
        var restrictionsListCommand = new Command("list", "List branch restrictions");
        restrictionsListCommand.SetHandler(async (string? workspace, string? repo) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            var restrictions = new List<object>();

            await foreach (var restriction in client.GetPaginatedAsync<JsonElement>($"/repositories/{workspace}/{repo}/branch-restrictions"))
            {
                restrictions.Add(new
                {
                    id = restriction.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                    kind = restriction.TryGetProperty("kind", out var k) ? k.GetString() : null,
                    pattern = restriction.TryGetProperty("pattern", out var p) ? p.GetString() : null,
                    branch_match_kind = restriction.TryGetProperty("branch_match_kind", out var bmk) ? bmk.GetString() : null
                });
            }

            Console.WriteLine(JsonSerializer.Serialize(new { count = restrictions.Count, restrictions }, JsonOptions));
        }, workspaceOption, repoOption);
        restrictionsCommand.AddCommand(restrictionsListCommand);

        // bbx branch restrictions add
        var restrictionsAddCommand = new Command("add", "Add a branch restriction");
        var kindOption = new Option<string>("--kind", "Restriction kind (push, force, delete, require_passing_builds_to_merge, require_approvals_to_merge, etc.)") { IsRequired = true };
        var patternOption = new Option<string>("--pattern", "Branch pattern (glob or exact match)") { IsRequired = true };
        restrictionsAddCommand.AddOption(kindOption);
        restrictionsAddCommand.AddOption(patternOption);
        restrictionsAddCommand.SetHandler(async (string? workspace, string? repo, string kind, string pattern) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            var body = new
            {
                kind,
                pattern,
                branch_match_kind = pattern.Contains('*') ? "glob" : "branching_model"
            };

            try
            {
                var result = await client.PostAsync<JsonElement>($"/repositories/{workspace}/{repo}/branch-restrictions", body);
                Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, kindOption, patternOption);
        restrictionsCommand.AddCommand(restrictionsAddCommand);

        // bbx branch restrictions delete
        var restrictionsDeleteCommand = new Command("delete", "Delete a branch restriction");
        var restrictionIdArg = new Argument<int>("id", "Restriction ID");
        restrictionsDeleteCommand.AddArgument(restrictionIdArg);
        restrictionsDeleteCommand.SetHandler(async (string? workspace, string? repo, int id) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            try
            {
                await client.DeleteAsync($"/repositories/{workspace}/{repo}/branch-restrictions/{id}");
                Console.WriteLine($"✓ Deleted branch restriction #{id}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, restrictionIdArg);
        restrictionsCommand.AddCommand(restrictionsDeleteCommand);

        command.AddCommand(restrictionsCommand);

        return command;
    }

    private static BitbucketClient CreateClient(BbxConfig config)
    {
        return new BitbucketClient(
            accessToken: config.AccessToken,
            appPassword: config.AppPassword,
            username: config.Username);
    }

    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };
}
