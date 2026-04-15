using System.CommandLine;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Commands;

public static class IssueCommand
{
    public static Command Create()
    {
        var workspaceOption = CommandOptions.CreateWorkspaceOption();
        var repoOption = CommandOptions.CreateRepoOption();
        var command = new Command("issue", "Manage issues");
        command.AddGlobalOption(workspaceOption);
        command.AddGlobalOption(repoOption);

        // bbx issue list
        var listCommand = new Command("list", "List issues");
        var stateOption = new Option<string?>("--state", "Filter by state (new, open, resolved, on hold, invalid, duplicate, wontfix, closed)");
        var priorityOption = new Option<string?>("--priority", "Filter by priority (trivial, minor, major, critical, blocker)");
        var assigneeOption = new Option<string?>("--assignee", "Filter by assignee account ID");
        var limitOption = new Option<int>("--limit", () => 25, "Maximum issues to list");
        listCommand.AddOption(stateOption);
        listCommand.AddOption(priorityOption);
        listCommand.AddOption(assigneeOption);
        listCommand.AddOption(limitOption);
        listCommand.SetHandler(async (string? workspace, string? repo, string? state, string? priority, string? assignee, int limit) =>
        {
            EmitDeprecationWarning();
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            var endpoint = $"/repositories/{workspace}/{repo}/issues";
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(state)) queryParams.Add($"q=state=\"{state}\"");
            if (!string.IsNullOrEmpty(priority)) queryParams.Add($"priority=\"{priority}\"");
            if (queryParams.Count > 0) endpoint += "?" + string.Join("&", queryParams);

            var count = 0;
            var issues = new List<object>();

            await foreach (var issue in client.GetPaginatedAsync<JsonElement>(endpoint))
            {
                issues.Add(ExtractIssueSummary(issue));
                if (++count >= limit) break;
            }

            var output = new { workspace, repository = repo, count = issues.Count, issues };
            Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
        }, workspaceOption, repoOption, stateOption, priorityOption, assigneeOption, limitOption);
        command.AddCommand(listCommand);

        // bbx issue view
        var viewCommand = new Command("view", "View issue details");
        var idArg = new Argument<int>("id", "Issue ID");
        viewCommand.AddArgument(idArg);
        viewCommand.SetHandler(async (string? workspace, string? repo, int id) =>
        {
            EmitDeprecationWarning();
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
                var issue = await client.GetAsync<JsonElement>($"/repositories/{workspace}/{repo}/issues/{id}");
                Console.WriteLine(JsonSerializer.Serialize(issue, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, idArg);
        command.AddCommand(viewCommand);

        // bbx issue create
        var createCommand = new Command("create", "Create a new issue");
        var titleOption = new Option<string>("--title", "Issue title") { IsRequired = true };
        var contentOption = new Option<string?>("--content", "Issue description");
        var kindOption = new Option<string?>("--kind", "Issue kind (bug, enhancement, proposal, task)");
        var priorityCreateOption = new Option<string?>("--priority", "Priority (trivial, minor, major, critical, blocker)");
        createCommand.AddOption(titleOption);
        createCommand.AddOption(contentOption);
        createCommand.AddOption(kindOption);
        createCommand.AddOption(priorityCreateOption);
        createCommand.SetHandler(async (string? workspace, string? repo, string title, string? content, string? kind, string? priority) =>
        {
            EmitDeprecationWarning();
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            var body = new Dictionary<string, object>
            {
                ["title"] = title
            };
            if (!string.IsNullOrEmpty(content)) body["content"] = new { raw = content };
            if (!string.IsNullOrEmpty(kind)) body["kind"] = kind;
            if (!string.IsNullOrEmpty(priority)) body["priority"] = priority;

            try
            {
                var result = await client.PostAsync<JsonElement>($"/repositories/{workspace}/{repo}/issues", body);
                Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, titleOption, contentOption, kindOption, priorityCreateOption);
        command.AddCommand(createCommand);

        // bbx issue update
        var updateCommand = new Command("update", "Update an issue");
        var updateIdArg = new Argument<int>("id", "Issue ID");
        var updateTitleOption = new Option<string?>("--title", "New title");
        var updateStateOption = new Option<string?>("--state", "New state");
        var updatePriorityOption = new Option<string?>("--priority", "New priority");
        var updateAssigneeOption = new Option<string?>("--assignee", "Assignee account ID");
        updateCommand.AddArgument(updateIdArg);
        updateCommand.AddOption(updateTitleOption);
        updateCommand.AddOption(updateStateOption);
        updateCommand.AddOption(updatePriorityOption);
        updateCommand.AddOption(updateAssigneeOption);
        updateCommand.SetHandler(async (string? workspace, string? repo, int id, string? title, string? state, string? priority, string? assignee) =>
        {
            EmitDeprecationWarning();
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            var body = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(title)) body["title"] = title;
            if (!string.IsNullOrEmpty(state)) body["state"] = state;
            if (!string.IsNullOrEmpty(priority)) body["priority"] = priority;
            if (!string.IsNullOrEmpty(assignee)) body["assignee"] = new { account_id = assignee };

            try
            {
                var result = await client.PutAsync<JsonElement>($"/repositories/{workspace}/{repo}/issues/{id}", body);
                Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, updateIdArg, updateTitleOption, updateStateOption, updatePriorityOption, updateAssigneeOption);
        command.AddCommand(updateCommand);

        // bbx issue delete
        var deleteCommand = new Command("delete", "Delete an issue");
        var deleteIdArg = new Argument<int>("id", "Issue ID");
        var yesOption = new Option<bool>("--yes", "Skip confirmation");
        deleteCommand.AddArgument(deleteIdArg);
        deleteCommand.AddOption(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string? repo, int id, bool yes) =>
        {
            EmitDeprecationWarning();
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            if (!yes)
            {
                Console.Write($"Delete issue #{id}? (y/N): ");
                if (Console.ReadLine()?.Trim().ToLower() != "y")
                {
                    Console.WriteLine("Cancelled.");
                    return;
                }
            }

            using var client = CreateClient(config);
            try
            {
                await client.DeleteAsync($"/repositories/{workspace}/{repo}/issues/{id}");
                Console.WriteLine($"✓ Deleted issue #{id}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, deleteIdArg, yesOption);
        command.AddCommand(deleteCommand);

        // bbx issue comments
        var commentsCommand = new Command("comments", "List issue comments");
        var commentsIdArg = new Argument<int>("id", "Issue ID");
        commentsCommand.AddArgument(commentsIdArg);
        commentsCommand.SetHandler(async (string? workspace, string? repo, int id) =>
        {
            EmitDeprecationWarning();
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            var comments = new List<object>();

            await foreach (var comment in client.GetPaginatedAsync<JsonElement>($"/repositories/{workspace}/{repo}/issues/{id}/comments"))
            {
                comments.Add(new
                {
                    id = comment.TryGetProperty("id", out var cid) ? cid.GetInt32() : 0,
                    user = comment.TryGetProperty("user", out var u) && u.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
                    content = comment.TryGetProperty("content", out var c) && c.TryGetProperty("raw", out var raw) ? raw.GetString() : null,
                    created_on = comment.TryGetProperty("created_on", out var co) ? co.GetString() : null
                });
            }

            Console.WriteLine(JsonSerializer.Serialize(new { issue_id = id, count = comments.Count, comments }, JsonOptions));
        }, workspaceOption, repoOption, commentsIdArg);
        command.AddCommand(commentsCommand);

        // bbx issue comment
        var commentCommand = new Command("comment", "Add a comment to an issue");
        var commentIdArg = new Argument<int>("id", "Issue ID");
        var commentBodyOption = new Option<string>("--body", "Comment text") { IsRequired = true };
        commentCommand.AddArgument(commentIdArg);
        commentCommand.AddOption(commentBodyOption);
        commentCommand.SetHandler(async (string? workspace, string? repo, int id, string commentBody) =>
        {
            EmitDeprecationWarning();
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            var body = new { content = new { raw = commentBody } };

            try
            {
                var result = await client.PostAsync<JsonElement>($"/repositories/{workspace}/{repo}/issues/{id}/comments", body);
                Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, commentIdArg, commentBodyOption);
        command.AddCommand(commentCommand);

        return command;
    }

    private static void EmitDeprecationWarning()
    {
        Console.Error.WriteLine("WARNING: Bitbucket Cloud Issues are being sunset by Atlassian.");
        Console.Error.WriteLine("  API endpoints will be removed on August 20, 2026.");
        Console.Error.WriteLine("  Migrate to Jira Software: https://support.atlassian.com/bitbucket-cloud/docs/export-or-import-issue-data/");
        Console.Error.WriteLine("  Full announcement: https://community.atlassian.com/forums/Bitbucket-articles/Announcing-sunset-of-Bitbucket-Issues-and-Wikis/ba-p/3193882");
        Console.Error.WriteLine();
    }

    private static object ExtractIssueSummary(JsonElement issue)
    {
        return new
        {
            id = issue.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
            title = issue.TryGetProperty("title", out var t) ? t.GetString() : null,
            state = issue.TryGetProperty("state", out var s) ? s.GetString() : null,
            kind = issue.TryGetProperty("kind", out var k) ? k.GetString() : null,
            priority = issue.TryGetProperty("priority", out var p) ? p.GetString() : null,
            reporter = issue.TryGetProperty("reporter", out var r) && r.TryGetProperty("display_name", out var rdn) ? rdn.GetString() : null,
            assignee = issue.TryGetProperty("assignee", out var a) && a.TryGetProperty("display_name", out var adn) ? adn.GetString() : null,
            created_on = issue.TryGetProperty("created_on", out var co) ? co.GetString() : null,
            updated_on = issue.TryGetProperty("updated_on", out var uo) ? uo.GetString() : null
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
