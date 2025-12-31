using System.CommandLine;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Commands;

public static class RepoCommand
{
    public static Command Create(Option<string?> workspaceOption)
    {
        var command = new Command("repo", "Manage repositories");
        command.AddOption(workspaceOption);

        // bbx repo list
        var listCommand = new Command("list", "List repositories");
        var limitOption = new Option<int>("--limit", () => 25, "Maximum repositories to list");
        var queryOption = new Option<string?>("--query", "BBQL query filter");
        listCommand.AddOption(limitOption);
        listCommand.AddOption(queryOption);
        listCommand.SetHandler(async (string? workspace, int limit, string? query) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace))
            {
                Console.Error.WriteLine("Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
                return;
            }

            using var client = CreateClient(config);
            var endpoint = $"/repositories/{workspace}";
            if (!string.IsNullOrEmpty(query))
            {
                endpoint += $"?q={Uri.EscapeDataString(query)}";
            }

            var count = 0;
            var repos = new List<object>();

            await foreach (var repo in client.GetPaginatedAsync<JsonElement>(endpoint))
            {
                repos.Add(new
                {
                    name = repo.TryGetProperty("name", out var n) ? n.GetString() : null,
                    slug = repo.TryGetProperty("slug", out var s) ? s.GetString() : null,
                    full_name = repo.TryGetProperty("full_name", out var fn) ? fn.GetString() : null,
                    is_private = repo.TryGetProperty("is_private", out var ip) && ip.GetBoolean(),
                    scm = repo.TryGetProperty("scm", out var scm) ? scm.GetString() : null,
                    description = repo.TryGetProperty("description", out var d) ? d.GetString() : null,
                    updated_on = repo.TryGetProperty("updated_on", out var u) ? u.GetString() : null,
                    size = repo.TryGetProperty("size", out var sz) ? sz.GetInt64() : 0
                });
                if (++count >= limit) break;
            }

            var output = new
            {
                workspace,
                count = repos.Count,
                repositories = repos
            };

            Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
        }, workspaceOption, limitOption, queryOption);
        command.AddCommand(listCommand);

        // bbx repo view
        var viewCommand = new Command("view", "View repository details");
        var repoArg = new Argument<string>("repository", "Repository (workspace/repo or just repo with --workspace)");
        viewCommand.AddArgument(repoArg);
        viewCommand.SetHandler(async (string? workspace, string repository) =>
        {
            var config = CredentialManager.Load();
            var (ws, repo) = ParseRepoPath(workspace ?? config.DefaultWorkspace, repository);

            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Invalid repository path. Use workspace/repo or set workspace.");
                return;
            }

            using var client = CreateClient(config);
            try
            {
                var result = await client.GetAsync<JsonElement>($"/repositories/{ws}/{repo}");
                Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoArg);
        command.AddCommand(viewCommand);

        // bbx repo create
        var createCommand = new Command("create", "Create a new repository");
        var nameArg = new Argument<string>("name", "Repository name");
        var privateOption = new Option<bool>("--private", "Create as private repository");
        var projectOption = new Option<string?>("--project", "Project key");
        var descOption = new Option<string?>("--description", "Repository description");
        var forkPolicyOption = new Option<string?>("--fork-policy", "Fork policy (allow_forks, no_public_forks, no_forks)");
        createCommand.AddArgument(nameArg);
        createCommand.AddOption(privateOption);
        createCommand.AddOption(projectOption);
        createCommand.AddOption(descOption);
        createCommand.AddOption(forkPolicyOption);
        createCommand.SetHandler(async (string? workspace, string name, bool isPrivate, string? project, string? description, string? forkPolicy) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace))
            {
                Console.Error.WriteLine("Error: Workspace required.");
                return;
            }

            using var client = CreateClient(config);
            var body = new Dictionary<string, object>
            {
                ["scm"] = "git",
                ["is_private"] = isPrivate,
                ["name"] = name
            };
            if (!string.IsNullOrEmpty(description)) body["description"] = description;
            if (!string.IsNullOrEmpty(project)) body["project"] = new { key = project };
            if (!string.IsNullOrEmpty(forkPolicy)) body["fork_policy"] = forkPolicy;

            var slug = name.ToLowerInvariant().Replace(' ', '-');

            try
            {
                var result = await client.PostAsync<JsonElement>($"/repositories/{workspace}/{slug}", body);
                Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, nameArg, privateOption, projectOption, descOption, forkPolicyOption);
        command.AddCommand(createCommand);

        // bbx repo delete
        var deleteCommand = new Command("delete", "Delete a repository");
        var deleteRepoArg = new Argument<string>("repository", "Repository to delete");
        var yesOption = new Option<bool>("--yes", "Skip confirmation");
        deleteCommand.AddArgument(deleteRepoArg);
        deleteCommand.AddOption(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string repository, bool yes) =>
        {
            var config = CredentialManager.Load();
            var (ws, repo) = ParseRepoPath(workspace ?? config.DefaultWorkspace, repository);

            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Invalid repository path.");
                return;
            }

            if (!yes)
            {
                Console.Write($"Delete {ws}/{repo}? This cannot be undone. (y/N): ");
                if (Console.ReadLine()?.Trim().ToLower() != "y")
                {
                    Console.WriteLine("Cancelled.");
                    return;
                }
            }

            using var client = CreateClient(config);
            try
            {
                await client.DeleteAsync($"/repositories/{ws}/{repo}");
                Console.WriteLine($"✓ Deleted {ws}/{repo}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, deleteRepoArg, yesOption);
        command.AddCommand(deleteCommand);

        // bbx repo fork
        var forkCommand = new Command("fork", "Fork a repository");
        var forkRepoArg = new Argument<string>("repository", "Repository to fork");
        var forkNameOption = new Option<string?>("--name", "Name for the forked repository");
        var forkWorkspaceOption = new Option<string?>("--to-workspace", "Destination workspace for fork");
        forkCommand.AddArgument(forkRepoArg);
        forkCommand.AddOption(forkNameOption);
        forkCommand.AddOption(forkWorkspaceOption);
        forkCommand.SetHandler(async (string? workspace, string repository, string? name, string? toWorkspace) =>
        {
            var config = CredentialManager.Load();
            var (ws, repo) = ParseRepoPath(workspace ?? config.DefaultWorkspace, repository);

            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Invalid repository path.");
                return;
            }

            using var client = CreateClient(config);
            var body = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(name)) body["name"] = name;
            if (!string.IsNullOrEmpty(toWorkspace)) body["workspace"] = new { slug = toWorkspace };

            try
            {
                var result = await client.PostAsync<JsonElement>($"/repositories/{ws}/{repo}/forks", body.Count > 0 ? body : null);
                Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, forkRepoArg, forkNameOption, forkWorkspaceOption);
        command.AddCommand(forkCommand);

        // bbx repo clone
        var cloneCommand = new Command("clone", "Get clone URL for a repository");
        var cloneRepoArg = new Argument<string>("repository", "Repository to clone");
        var sshOption = new Option<bool>("--ssh", "Get SSH URL instead of HTTPS");
        cloneCommand.AddArgument(cloneRepoArg);
        cloneCommand.AddOption(sshOption);
        cloneCommand.SetHandler(async (string? workspace, string repository, bool ssh) =>
        {
            var config = CredentialManager.Load();
            var (ws, repo) = ParseRepoPath(workspace ?? config.DefaultWorkspace, repository);

            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Invalid repository path.");
                return;
            }

            using var client = CreateClient(config);
            try
            {
                var result = await client.GetAsync<JsonElement>($"/repositories/{ws}/{repo}");
                if (result.TryGetProperty("links", out var links) && links.TryGetProperty("clone", out var cloneLinks))
                {
                    foreach (var link in cloneLinks.EnumerateArray())
                    {
                        var name = link.GetProperty("name").GetString();
                        var href = link.GetProperty("href").GetString();
                        if ((ssh && name == "ssh") || (!ssh && name == "https"))
                        {
                            Console.WriteLine(href);
                            return;
                        }
                    }
                }
                Console.Error.WriteLine("Error: Clone URL not found");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, cloneRepoArg, sshOption);
        command.AddCommand(cloneCommand);

        // bbx repo permissions
        var permissionsCommand = new Command("permissions", "View repository permissions");
        var permRepoArg = new Argument<string>("repository", "Repository");
        permissionsCommand.AddArgument(permRepoArg);
        permissionsCommand.SetHandler(async (string? workspace, string repository) =>
        {
            var config = CredentialManager.Load();
            var (ws, repo) = ParseRepoPath(workspace ?? config.DefaultWorkspace, repository);

            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Invalid repository path.");
                return;
            }

            using var client = CreateClient(config);
            try
            {
                var permissions = new List<object>();
                await foreach (var perm in client.GetPaginatedAsync<JsonElement>($"/repositories/{ws}/{repo}/permissions-config/users"))
                {
                    permissions.Add(new
                    {
                        type = "user",
                        user = perm.TryGetProperty("user", out var u) ? u.GetProperty("display_name").GetString() : null,
                        permission = perm.TryGetProperty("permission", out var p) ? p.GetString() : null
                    });
                }

                Console.WriteLine(JsonSerializer.Serialize(new { count = permissions.Count, permissions }, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, permRepoArg);
        command.AddCommand(permissionsCommand);

        return command;
    }

    private static BitbucketClient CreateClient(BbxConfig config)
    {
        return new BitbucketClient(
            accessToken: config.AccessToken,
            appPassword: config.AppPassword,
            username: config.Username);
    }

    private static (string? workspace, string? repo) ParseRepoPath(string? defaultWorkspace, string path)
    {
        if (path.Contains('/'))
        {
            var parts = path.Split('/', 2);
            return (parts[0], parts[1]);
        }
        return (defaultWorkspace, path);
    }

    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };
}
