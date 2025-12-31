using System.CommandLine;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Commands;

public static class PrCommand
{
    public static Command Create(Option<string?> workspaceOption, Option<string?> repoOption)
    {
        var command = new Command("pr", "Manage pull requests");
        command.AddOption(workspaceOption);
        command.AddOption(repoOption);

        // bbx pr list
        var listCommand = new Command("list", "List pull requests");
        var stateOption = new Option<string?>("--state", "Filter by state (OPEN, MERGED, DECLINED, SUPERSEDED)");
        var authorOption = new Option<string?>("--author", "Filter by author account ID");
        var limitOption = new Option<int>("--limit", () => 25, "Maximum PRs to list");
        listCommand.AddOption(stateOption);
        listCommand.AddOption(authorOption);
        listCommand.AddOption(limitOption);
        listCommand.SetHandler(async (string? workspace, string? repo, string? state, string? author, int limit) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required. Use --workspace and --repo options.");
                return;
            }

            using var client = CreateClient(config);
            var endpoint = $"/repositories/{workspace}/{repo}/pullrequests";
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(state)) queryParams.Add($"state={state.ToUpper()}");
            if (queryParams.Count > 0) endpoint += "?" + string.Join("&", queryParams);

            var count = 0;
            var prs = new List<object>();

            await foreach (var pr in client.GetPaginatedAsync<JsonElement>(endpoint))
            {
                prs.Add(ExtractPrSummary(pr));
                if (++count >= limit) break;
            }

            var output = new { workspace, repository = repo, count = prs.Count, pull_requests = prs };
            Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
        }, workspaceOption, repoOption, stateOption, authorOption, limitOption);
        command.AddCommand(listCommand);

        // bbx pr view
        var viewCommand = new Command("view", "View pull request details");
        var idArg = new Argument<int>("id", "Pull request ID");
        viewCommand.AddArgument(idArg);
        viewCommand.SetHandler(async (string? workspace, string? repo, int id) =>
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
                var pr = await client.GetAsync<JsonElement>($"/repositories/{workspace}/{repo}/pullrequests/{id}");
                Console.WriteLine(JsonSerializer.Serialize(pr, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, idArg);
        command.AddCommand(viewCommand);

        // bbx pr create
        var createCommand = new Command("create", "Create a new pull request");
        var titleOption = new Option<string>("--title", "Pull request title") { IsRequired = true };
        var sourceOption = new Option<string>("--source", "Source branch") { IsRequired = true };
        var destOption = new Option<string>("--dest", "Destination branch") { IsRequired = true };
        var bodyOption = new Option<string?>("--body", "Pull request description");
        var reviewersOption = new Option<string[]?>("--reviewers", "Reviewer account IDs (UUID format)");
        var closeSourceOption = new Option<bool>("--close-source-branch", "Close source branch after merge");
        createCommand.AddOption(titleOption);
        createCommand.AddOption(sourceOption);
        createCommand.AddOption(destOption);
        createCommand.AddOption(bodyOption);
        createCommand.AddOption(reviewersOption);
        createCommand.AddOption(closeSourceOption);
        createCommand.SetHandler(async (string? workspace, string? repo, string title, string source, string dest, string? body, string[]? reviewers, bool closeSource) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            var prBody = new Dictionary<string, object>
            {
                ["title"] = title,
                ["source"] = new { branch = new { name = source } },
                ["destination"] = new { branch = new { name = dest } },
                ["close_source_branch"] = closeSource
            };

            if (!string.IsNullOrEmpty(body)) prBody["description"] = body;
            if (reviewers?.Length > 0)
            {
                prBody["reviewers"] = reviewers.Select(r => new { account_id = r }).ToArray();
            }

            try
            {
                var result = await client.PostAsync<JsonElement>($"/repositories/{workspace}/{repo}/pullrequests", prBody);
                Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, titleOption, sourceOption, destOption, bodyOption, reviewersOption, closeSourceOption);
        command.AddCommand(createCommand);

        // bbx pr merge
        var mergeCommand = new Command("merge", "Merge a pull request");
        var mergeIdArg = new Argument<int>("id", "Pull request ID");
        var strategyOption = new Option<string>("--strategy", () => "merge", "Merge strategy (merge, squash, fast_forward)");
        var messageOption = new Option<string?>("--message", "Merge commit message");
        var closeSourceMergeOption = new Option<bool>("--close-source-branch", "Close source branch after merge");
        mergeCommand.AddArgument(mergeIdArg);
        mergeCommand.AddOption(strategyOption);
        mergeCommand.AddOption(messageOption);
        mergeCommand.AddOption(closeSourceMergeOption);
        mergeCommand.SetHandler(async (string? workspace, string? repo, int id, string strategy, string? message, bool closeSource) =>
        {
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
                ["type"] = "pullrequest",
                ["merge_strategy"] = strategy,
                ["close_source_branch"] = closeSource
            };
            if (!string.IsNullOrEmpty(message)) body["message"] = message;

            try
            {
                var result = await client.PostAsync<JsonElement>($"/repositories/{workspace}/{repo}/pullrequests/{id}/merge", body);
                Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, mergeIdArg, strategyOption, messageOption, closeSourceMergeOption);
        command.AddCommand(mergeCommand);

        // bbx pr approve
        var approveCommand = new Command("approve", "Approve a pull request");
        var approveIdArg = new Argument<int>("id", "Pull request ID");
        approveCommand.AddArgument(approveIdArg);
        approveCommand.SetHandler(async (string? workspace, string? repo, int id) =>
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
                await client.PostAsync<JsonElement>($"/repositories/{workspace}/{repo}/pullrequests/{id}/approve", null);
                Console.WriteLine($"✓ Approved PR #{id}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, approveIdArg);
        command.AddCommand(approveCommand);

        // bbx pr unapprove
        var unapproveCommand = new Command("unapprove", "Remove approval from a pull request");
        var unapproveIdArg = new Argument<int>("id", "Pull request ID");
        unapproveCommand.AddArgument(unapproveIdArg);
        unapproveCommand.SetHandler(async (string? workspace, string? repo, int id) =>
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
                await client.DeleteAsync($"/repositories/{workspace}/{repo}/pullrequests/{id}/approve");
                Console.WriteLine($"✓ Removed approval from PR #{id}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, unapproveIdArg);
        command.AddCommand(unapproveCommand);

        // bbx pr decline
        var declineCommand = new Command("decline", "Decline a pull request");
        var declineIdArg = new Argument<int>("id", "Pull request ID");
        var declineReasonOption = new Option<string?>("--reason", "Reason for declining");
        declineCommand.AddArgument(declineIdArg);
        declineCommand.AddOption(declineReasonOption);
        declineCommand.SetHandler(async (string? workspace, string? repo, int id, string? reason) =>
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
                var body = !string.IsNullOrEmpty(reason) ? new { reason } : null;
                await client.PostAsync<JsonElement>($"/repositories/{workspace}/{repo}/pullrequests/{id}/decline", body);
                Console.WriteLine($"✓ Declined PR #{id}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, declineIdArg, declineReasonOption);
        command.AddCommand(declineCommand);

        // bbx pr comments
        var commentsCommand = new Command("comments", "List pull request comments");
        var commentsIdArg = new Argument<int>("id", "Pull request ID");
        commentsCommand.AddArgument(commentsIdArg);
        commentsCommand.SetHandler(async (string? workspace, string? repo, int id) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            var comments = new List<object>();

            await foreach (var comment in client.GetPaginatedAsync<JsonElement>($"/repositories/{workspace}/{repo}/pullrequests/{id}/comments"))
            {
                comments.Add(new
                {
                    id = comment.TryGetProperty("id", out var cid) ? cid.GetInt32() : 0,
                    user = comment.TryGetProperty("user", out var u) && u.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
                    content = comment.TryGetProperty("content", out var c) && c.TryGetProperty("raw", out var raw) ? raw.GetString() : null,
                    created_on = comment.TryGetProperty("created_on", out var co) ? co.GetString() : null,
                    inline = comment.TryGetProperty("inline", out _)
                });
            }

            Console.WriteLine(JsonSerializer.Serialize(new { pull_request_id = id, count = comments.Count, comments }, JsonOptions));
        }, workspaceOption, repoOption, commentsIdArg);
        command.AddCommand(commentsCommand);

        // bbx pr comment
        var commentCommand = new Command("comment", "Add a comment to a pull request");
        var commentIdArg = new Argument<int>("id", "Pull request ID");
        var commentBodyOption = new Option<string>("--body", "Comment text") { IsRequired = true };
        commentCommand.AddArgument(commentIdArg);
        commentCommand.AddOption(commentBodyOption);
        commentCommand.SetHandler(async (string? workspace, string? repo, int id, string commentBody) =>
        {
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
                var result = await client.PostAsync<JsonElement>($"/repositories/{workspace}/{repo}/pullrequests/{id}/comments", body);
                Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, commentIdArg, commentBodyOption);
        command.AddCommand(commentCommand);

        // bbx pr diff
        var diffCommand = new Command("diff", "Show pull request diff");
        var diffIdArg = new Argument<int>("id", "Pull request ID");
        diffCommand.AddArgument(diffIdArg);
        diffCommand.SetHandler(async (string? workspace, string? repo, int id) =>
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
                var diff = await client.GetStringAsync($"/repositories/{workspace}/{repo}/pullrequests/{id}/diff");
                Console.WriteLine(diff);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }, workspaceOption, repoOption, diffIdArg);
        command.AddCommand(diffCommand);

        // bbx pr activity
        var activityCommand = new Command("activity", "Show pull request activity log");
        var activityIdArg = new Argument<int>("id", "Pull request ID");
        activityCommand.AddArgument(activityIdArg);
        activityCommand.SetHandler(async (string? workspace, string? repo, int id) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            var activities = new List<object>();

            await foreach (var activity in client.GetPaginatedAsync<JsonElement>($"/repositories/{workspace}/{repo}/pullrequests/{id}/activity"))
            {
                activities.Add(activity);
            }

            Console.WriteLine(JsonSerializer.Serialize(new { pull_request_id = id, count = activities.Count, activities }, JsonOptions));
        }, workspaceOption, repoOption, activityIdArg);
        command.AddCommand(activityCommand);

        // bbx pr statuses
        var statusesCommand = new Command("statuses", "Show pull request commit statuses");
        var statusesIdArg = new Argument<int>("id", "Pull request ID");
        statusesCommand.AddArgument(statusesIdArg);
        statusesCommand.SetHandler(async (string? workspace, string? repo, int id) =>
        {
            var config = CredentialManager.Load();
            workspace ??= config.DefaultWorkspace;

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            {
                Console.Error.WriteLine("Error: Workspace and repository required.");
                return;
            }

            using var client = CreateClient(config);
            var statuses = new List<object>();

            await foreach (var status in client.GetPaginatedAsync<JsonElement>($"/repositories/{workspace}/{repo}/pullrequests/{id}/statuses"))
            {
                statuses.Add(new
                {
                    key = status.TryGetProperty("key", out var k) ? k.GetString() : null,
                    state = status.TryGetProperty("state", out var s) ? s.GetString() : null,
                    name = status.TryGetProperty("name", out var n) ? n.GetString() : null,
                    url = status.TryGetProperty("url", out var u) ? u.GetString() : null,
                    description = status.TryGetProperty("description", out var d) ? d.GetString() : null
                });
            }

            Console.WriteLine(JsonSerializer.Serialize(new { pull_request_id = id, count = statuses.Count, statuses }, JsonOptions));
        }, workspaceOption, repoOption, statusesIdArg);
        command.AddCommand(statusesCommand);

        return command;
    }

    private static object ExtractPrSummary(JsonElement pr)
    {
        return new
        {
            id = pr.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
            title = pr.TryGetProperty("title", out var title) ? title.GetString() : null,
            state = pr.TryGetProperty("state", out var state) ? state.GetString() : null,
            author = pr.TryGetProperty("author", out var author) && author.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
            source = pr.TryGetProperty("source", out var src) && src.TryGetProperty("branch", out var sb) && sb.TryGetProperty("name", out var sn) ? sn.GetString() : null,
            destination = pr.TryGetProperty("destination", out var dest) && dest.TryGetProperty("branch", out var db) && db.TryGetProperty("name", out var destName) ? destName.GetString() : null,
            created_on = pr.TryGetProperty("created_on", out var co) ? co.GetString() : null,
            updated_on = pr.TryGetProperty("updated_on", out var uo) ? uo.GetString() : null,
            comment_count = pr.TryGetProperty("comment_count", out var cc) ? cc.GetInt32() : 0,
            task_count = pr.TryGetProperty("task_count", out var tc) ? tc.GetInt32() : 0
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
