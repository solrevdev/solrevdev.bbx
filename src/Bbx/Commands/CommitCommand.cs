using System.CommandLine;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Commands;

public static class CommitCommand
{
    public static Command Create()
    {
        var workspaceOption = CommandOptions.CreateWorkspaceOption();
        var repoOption = CommandOptions.CreateRepoOption();
        var command = new Command("commit", "View commits and commit details");
        command.AddGlobalOption(workspaceOption);
        command.AddGlobalOption(repoOption);

        // bbx commit list
        var listCommand = new Command("list", "List commits");
        var branchOption = new Option<string?>("--branch", "Filter by branch name");
        var pathOption = new Option<string?>("--path", "Filter by file path");
        var limitOption = new Option<int>("--limit", () => 25, "Maximum commits to list");
        listCommand.AddOption(branchOption);
        listCommand.AddOption(pathOption);
        listCommand.AddOption(limitOption);
        listCommand.SetHandler(async (string? workspace, string? repo, string? branch, string? path, int limit) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                Environment.ExitCode = 1;
                return;
            }

            using var client = CreateClient(config);
            var endpoint = $"/repositories/{workspace}/{repo}/commits";
            if (!string.IsNullOrEmpty(branch))
            {
                endpoint = $"/repositories/{workspace}/{repo}/commits/{Uri.EscapeDataString(branch)}";
            }

            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(path)) queryParams.Add($"path={Uri.EscapeDataString(path)}");
            if (queryParams.Count > 0) endpoint += "?" + string.Join("&", queryParams);

            var count = 0;
            var commits = new List<object>();

            await foreach (var commit in client.GetPaginatedAsync<JsonElement>(endpoint))
            {
                commits.Add(ExtractCommitSummary(commit));
                if (++count >= limit) break;
            }

            var output = new { workspace, repository = repo, count = commits.Count, commits };
            Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
        }, workspaceOption, repoOption, branchOption, pathOption, limitOption);
        command.AddCommand(listCommand);

        // bbx commit view
        var viewCommand = new Command("view", "View commit details");
        var hashArg = new Argument<string>("hash", "Commit hash");
        viewCommand.AddArgument(hashArg);
        viewCommand.SetHandler(async (string? workspace, string? repo, string hash) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                Environment.ExitCode = 1;
                return;
            }

            using var client = CreateClient(config);
            try
            {
                var commit = await client.GetAsync<JsonElement>($"/repositories/{workspace}/{repo}/commit/{hash}");
                Console.WriteLine(JsonSerializer.Serialize(commit, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, hashArg);
        command.AddCommand(viewCommand);

        // bbx commit diff
        var diffCommand = new Command("diff", "Show commit diff");
        var diffHashArg = new Argument<string>("hash", "Commit hash");
        diffCommand.AddArgument(diffHashArg);
        diffCommand.SetHandler(async (string? workspace, string? repo, string hash) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                Environment.ExitCode = 1;
                return;
            }

            using var client = CreateClient(config);
            try
            {
                var diff = await client.GetStringAsync($"/repositories/{workspace}/{repo}/diff/{hash}");
                Console.WriteLine(diff);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, diffHashArg);
        command.AddCommand(diffCommand);

        // bbx commit patch
        var patchCommand = new Command("patch", "Show commit as patch");
        var patchHashArg = new Argument<string>("hash", "Commit hash");
        patchCommand.AddArgument(patchHashArg);
        patchCommand.SetHandler(async (string? workspace, string? repo, string hash) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                Environment.ExitCode = 1;
                return;
            }

            using var client = CreateClient(config);
            try
            {
                var patch = await client.GetStringAsync($"/repositories/{workspace}/{repo}/patch/{hash}");
                Console.WriteLine(patch);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, patchHashArg);
        command.AddCommand(patchCommand);

        // bbx commit comments
        var commentsCommand = new Command("comments", "List commit comments");
        var commentsHashArg = new Argument<string>("hash", "Commit hash");
        commentsCommand.AddArgument(commentsHashArg);
        commentsCommand.SetHandler(async (string? workspace, string? repo, string hash) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                Environment.ExitCode = 1;
                return;
            }

            using var client = CreateClient(config);
            var comments = new List<object>();

            await foreach (var comment in client.GetPaginatedAsync<JsonElement>($"/repositories/{workspace}/{repo}/commit/{hash}/comments"))
            {
                comments.Add(new
                {
                    id = comment.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                    user = comment.TryGetObject("user", out var u) && u.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
                    content = comment.TryGetObject("content", out var c) && c.TryGetProperty("raw", out var raw) ? raw.GetString() : null,
                    created_on = comment.TryGetProperty("created_on", out var co) ? co.GetString() : null
                });
            }

            Console.WriteLine(JsonSerializer.Serialize(new { commit = hash, count = comments.Count, comments }, JsonOptions));
        }, workspaceOption, repoOption, commentsHashArg);
        command.AddCommand(commentsCommand);

        // bbx commit statuses
        var statusesCommand = new Command("statuses", "List commit build statuses");
        var statusesHashArg = new Argument<string>("hash", "Commit hash");
        statusesCommand.AddArgument(statusesHashArg);
        statusesCommand.SetHandler(async (string? workspace, string? repo, string hash) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                Environment.ExitCode = 1;
                return;
            }

            using var client = CreateClient(config);
            var statuses = new List<object>();

            await foreach (var status in client.GetPaginatedAsync<JsonElement>($"/repositories/{workspace}/{repo}/commit/{hash}/statuses"))
            {
                statuses.Add(new
                {
                    key = status.TryGetProperty("key", out var k) ? k.GetString() : null,
                    state = status.TryGetProperty("state", out var s) ? s.GetString() : null,
                    name = status.TryGetProperty("name", out var n) ? n.GetString() : null,
                    url = status.TryGetProperty("url", out var u) ? u.GetString() : null,
                    description = status.TryGetProperty("description", out var d) ? d.GetString() : null,
                    created_on = status.TryGetProperty("created_on", out var co) ? co.GetString() : null
                });
            }

            Console.WriteLine(JsonSerializer.Serialize(new { commit = hash, count = statuses.Count, statuses }, JsonOptions));
        }, workspaceOption, repoOption, statusesHashArg);
        command.AddCommand(statusesCommand);

        // bbx commit pullrequests
        var prsCommand = new Command("pullrequests", "List pull requests for a commit");
        var prsHashArg = new Argument<string>("hash", "Commit hash");
        prsCommand.AddArgument(prsHashArg);
        prsCommand.SetHandler(async (string? workspace, string? repo, string hash) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                Environment.ExitCode = 1;
                return;
            }

            using var client = CreateClient(config);
            var prs = new List<object>();

            await foreach (var pr in client.GetPaginatedAsync<JsonElement>($"/repositories/{workspace}/{repo}/commit/{hash}/pullrequests"))
            {
                prs.Add(new
                {
                    id = pr.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                    title = pr.TryGetProperty("title", out var t) ? t.GetString() : null,
                    state = pr.TryGetProperty("state", out var s) ? s.GetString() : null
                });
            }

            Console.WriteLine(JsonSerializer.Serialize(new { commit = hash, count = prs.Count, pull_requests = prs }, JsonOptions));
        }, workspaceOption, repoOption, prsHashArg);
        command.AddCommand(prsCommand);

        return command;
    }

    private static object ExtractCommitSummary(JsonElement commit)
    {
        return new
        {
            hash = commit.TryGetProperty("hash", out var h) ? h.GetString()?[..12] : null,
            full_hash = commit.TryGetProperty("hash", out var fh) ? fh.GetString() : null,
            message = commit.TryGetProperty("message", out var m) ? m.GetString()?.Split('\n')[0] : null,
            author = commit.TryGetObject("author", out var a) && a.TryGetObject("user", out var u) && u.TryGetProperty("display_name", out var dn) ? dn.GetString() :
                     commit.TryGetObject("author", out var a2) && a2.TryGetProperty("raw", out var raw) ? raw.GetString() : null,
            date = commit.TryGetProperty("date", out var d) ? d.GetString() : null
        };
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
