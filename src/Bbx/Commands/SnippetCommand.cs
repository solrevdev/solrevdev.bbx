using System.CommandLine;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Commands;

public static class SnippetCommand
{
    public static Command Create()
    {
        var command = new Command("snippet", "Manage Bitbucket snippets");

        command.AddCommand(CreateListCommand());
        command.AddCommand(CreateViewCommand());
        command.AddCommand(CreateCreateCommand());
        command.AddCommand(CreateUpdateCommand());
        command.AddCommand(CreateDeleteCommand());
        command.AddCommand(CreateFilesCommand());
        command.AddCommand(CreateWatchCommand());
        command.AddCommand(CreateCommentsCommand());

        return command;
    }

    private static Command CreateListCommand()
    {
        var command = new Command("list", "List snippets");

        var workspaceOption = new Option<string?>(
            ["--workspace", "-w"],
            "Workspace slug (uses default if not specified, omit for personal snippets)");

        var roleOption = new Option<string?>(
            ["--role", "-r"],
            "Filter by role: owner, contributor, member");

        var limitOption = new Option<int>(
            ["--limit", "-l"],
            () => 25,
            "Maximum number of snippets to return");

        command.AddOption(workspaceOption);
        command.AddOption(roleOption);
        command.AddOption(limitOption);

        command.SetHandler(async (workspace, role, limit) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials?.AccessToken is null && credentials?.AppPassword is null)
            {
                Console.Error.WriteLine("Error: Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                string endpoint;
                if (!string.IsNullOrEmpty(workspace))
                {
                    endpoint = $"snippets/{workspace}";
                }
                else
                {
                    endpoint = "snippets";
                }

                if (!string.IsNullOrEmpty(role))
                {
                    endpoint += $"?role={role}";
                }

                var snippets = new List<object>();
                await foreach (var snippet in client.GetPaginatedAsync<JsonElement>(endpoint))
                {
                    snippets.Add(new
                    {
                        id = snippet.GetProperty("id").GetString(),
                        title = snippet.TryGetProperty("title", out var t) ? t.GetString() : null,
                        is_private = snippet.TryGetProperty("is_private", out var p) && p.GetBoolean(),
                        scm = snippet.TryGetProperty("scm", out var s) ? s.GetString() : "git",
                        created_on = snippet.TryGetProperty("created_on", out var c) ? c.GetString() : null,
                        updated_on = snippet.TryGetProperty("updated_on", out var u) ? u.GetString() : null,
                        owner = snippet.TryGetProperty("owner", out var o) && o.TryGetProperty("display_name", out var d) ? d.GetString() : null
                    });

                    if (snippets.Count >= limit) break;
                }

                Console.WriteLine(JsonSerializer.Serialize(new { snippets, count = snippets.Count },
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, roleOption, limitOption);

        return command;
    }

    private static Command CreateViewCommand()
    {
        var command = new Command("view", "View a snippet");

        var snippetIdArg = new Argument<string>("snippet-id", "Snippet ID");

        var workspaceOption = new Option<string?>(
            ["--workspace", "-w"],
            "Workspace slug (uses default if not specified)");

        command.AddArgument(snippetIdArg);
        command.AddOption(workspaceOption);

        command.SetHandler(async (snippetId, workspace) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials?.AccessToken is null && credentials?.AppPassword is null)
            {
                Console.Error.WriteLine("Error: Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            workspace ??= credentials.DefaultWorkspace;
            if (string.IsNullOrEmpty(workspace))
            {
                Console.Error.WriteLine("Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                var snippet = await client.GetAsync<JsonElement>($"snippets/{workspace}/{snippetId}");

                var result = new
                {
                    id = snippet.GetProperty("id").GetString(),
                    title = snippet.TryGetProperty("title", out var t) ? t.GetString() : null,
                    is_private = snippet.TryGetProperty("is_private", out var p) && p.GetBoolean(),
                    scm = snippet.TryGetProperty("scm", out var s) ? s.GetString() : "git",
                    created_on = snippet.TryGetProperty("created_on", out var c) ? c.GetString() : null,
                    updated_on = snippet.TryGetProperty("updated_on", out var u) ? u.GetString() : null,
                    owner = snippet.TryGetProperty("owner", out var o) ? new
                    {
                        display_name = o.TryGetProperty("display_name", out var d) ? d.GetString() : null,
                        username = o.TryGetProperty("username", out var un) ? un.GetString() : null
                    } : null,
                    creator = snippet.TryGetProperty("creator", out var cr) ? new
                    {
                        display_name = cr.TryGetProperty("display_name", out var cd) ? cd.GetString() : null,
                        username = cr.TryGetProperty("username", out var cu) ? cu.GetString() : null
                    } : null
                };

                Console.WriteLine(JsonSerializer.Serialize(result,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, snippetIdArg, workspaceOption);

        return command;
    }

    private static Command CreateCreateCommand()
    {
        var command = new Command("create", "Create a new snippet");

        var titleOption = new Option<string>(
            ["--title", "-t"],
            "Snippet title")
        { IsRequired = true };

        var fileOption = new Option<string[]>(
            ["--file", "-f"],
            "File(s) to include in snippet (can be specified multiple times)")
        { IsRequired = true, AllowMultipleArgumentsPerToken = true };

        var privateOption = new Option<bool>(
            ["--private", "-p"],
            () => false,
            "Make snippet private");

        var workspaceOption = new Option<string?>(
            ["--workspace", "-w"],
            "Workspace slug (uses default if not specified)");

        command.AddOption(titleOption);
        command.AddOption(fileOption);
        command.AddOption(privateOption);
        command.AddOption(workspaceOption);

        command.SetHandler(async (title, files, isPrivate, workspace) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials?.AccessToken is null && credentials?.AppPassword is null)
            {
                Console.Error.WriteLine("Error: Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            workspace ??= credentials.DefaultWorkspace;
            if (string.IsNullOrEmpty(workspace))
            {
                Console.Error.WriteLine("Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                // Build multipart form data for snippet creation
                using var content = new MultipartFormDataContent();

                content.Add(new StringContent(title), "title");
                content.Add(new StringContent(isPrivate.ToString().ToLower()), "is_private");

                foreach (var filePath in files)
                {
                    if (!File.Exists(filePath))
                    {
                        Console.Error.WriteLine($"Error: File not found: {filePath}");
                        Environment.ExitCode = 1;
                        return;
                    }

                    var fileName = Path.GetFileName(filePath);
                    var fileContent = await File.ReadAllBytesAsync(filePath);
                    content.Add(new ByteArrayContent(fileContent), $"file", fileName);
                }

                var snippet = await client.PostMultipartAsync<JsonElement>($"snippets/{workspace}", content);

                var result = new
                {
                    id = snippet.GetProperty("id").GetString(),
                    title = snippet.TryGetProperty("title", out var t) ? t.GetString() : null,
                    is_private = snippet.TryGetProperty("is_private", out var p) && p.GetBoolean(),
                    created_on = snippet.TryGetProperty("created_on", out var c) ? c.GetString() : null,
                    links = snippet.TryGetProperty("links", out var l) && l.TryGetProperty("html", out var h) && h.TryGetProperty("href", out var href)
                        ? href.GetString() : null
                };

                Console.WriteLine(JsonSerializer.Serialize(result,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, titleOption, fileOption, privateOption, workspaceOption);

        return command;
    }

    private static Command CreateUpdateCommand()
    {
        var command = new Command("update", "Update an existing snippet");

        var snippetIdArg = new Argument<string>("snippet-id", "Snippet ID");

        var titleOption = new Option<string?>(
            ["--title", "-t"],
            "New title");

        var fileOption = new Option<string[]?>(
            ["--file", "-f"],
            "File(s) to update/add (can be specified multiple times)");

        var privateOption = new Option<bool?>(
            ["--private", "-p"],
            "Make snippet private");

        var workspaceOption = new Option<string?>(
            ["--workspace", "-w"],
            "Workspace slug (uses default if not specified)");

        command.AddArgument(snippetIdArg);
        command.AddOption(titleOption);
        command.AddOption(fileOption);
        command.AddOption(privateOption);
        command.AddOption(workspaceOption);

        command.SetHandler(async (snippetId, title, files, isPrivate, workspace) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials?.AccessToken is null && credentials?.AppPassword is null)
            {
                Console.Error.WriteLine("Error: Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            workspace ??= credentials.DefaultWorkspace;
            if (string.IsNullOrEmpty(workspace))
            {
                Console.Error.WriteLine("Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                using var content = new MultipartFormDataContent();

                if (!string.IsNullOrEmpty(title))
                {
                    content.Add(new StringContent(title), "title");
                }

                if (isPrivate.HasValue)
                {
                    content.Add(new StringContent(isPrivate.Value.ToString().ToLower()), "is_private");
                }

                if (files is not null)
                {
                    foreach (var filePath in files)
                    {
                        if (!File.Exists(filePath))
                        {
                            Console.Error.WriteLine($"Error: File not found: {filePath}");
                            Environment.ExitCode = 1;
                            return;
                        }

                        var fileName = Path.GetFileName(filePath);
                        var fileContent = await File.ReadAllBytesAsync(filePath);
                        content.Add(new ByteArrayContent(fileContent), $"file", fileName);
                    }
                }

                var snippet = await client.PutMultipartAsync<JsonElement>($"snippets/{workspace}/{snippetId}", content);

                var result = new
                {
                    id = snippet.GetProperty("id").GetString(),
                    title = snippet.TryGetProperty("title", out var t) ? t.GetString() : null,
                    is_private = snippet.TryGetProperty("is_private", out var p) && p.GetBoolean(),
                    updated_on = snippet.TryGetProperty("updated_on", out var u) ? u.GetString() : null
                };

                Console.WriteLine(JsonSerializer.Serialize(result,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, snippetIdArg, titleOption, fileOption, privateOption, workspaceOption);

        return command;
    }

    private static Command CreateDeleteCommand()
    {
        var command = new Command("delete", "Delete a snippet");

        var snippetIdArg = new Argument<string>("snippet-id", "Snippet ID");

        var workspaceOption = new Option<string?>(
            ["--workspace", "-w"],
            "Workspace slug (uses default if not specified)");

        var yesOption = new Option<bool>(
            ["--yes", "-y"],
            () => false,
            "Skip confirmation prompt");

        command.AddArgument(snippetIdArg);
        command.AddOption(workspaceOption);
        command.AddOption(yesOption);

        command.SetHandler(async (snippetId, workspace, yes) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials?.AccessToken is null && credentials?.AppPassword is null)
            {
                Console.Error.WriteLine("Error: Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            workspace ??= credentials.DefaultWorkspace;
            if (string.IsNullOrEmpty(workspace))
            {
                Console.Error.WriteLine("Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
                Environment.ExitCode = 1;
                return;
            }

            if (!yes)
            {
                Console.Error.Write($"Delete snippet '{snippetId}'? [y/N]: ");
                var response = Console.ReadLine()?.Trim().ToLower();
                if (response != "y" && response != "yes")
                {
                    Console.Error.WriteLine("Cancelled.");
                    return;
                }
            }

            var client = new BitbucketClient(credentials);

            try
            {
                await client.DeleteAsync($"snippets/{workspace}/{snippetId}");
                Console.WriteLine(JsonSerializer.Serialize(new { deleted = true, snippet_id = snippetId },
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, snippetIdArg, workspaceOption, yesOption);

        return command;
    }

    private static Command CreateFilesCommand()
    {
        var command = new Command("files", "List or get files in a snippet");

        var snippetIdArg = new Argument<string>("snippet-id", "Snippet ID");

        var fileNameArg = new Argument<string?>("file-name", () => null, "File name to retrieve (optional)");

        var workspaceOption = new Option<string?>(
            ["--workspace", "-w"],
            "Workspace slug (uses default if not specified)");

        var rawOption = new Option<bool>(
            ["--raw", "-r"],
            () => false,
            "Output raw file content (only when file-name specified)");

        command.AddArgument(snippetIdArg);
        command.AddArgument(fileNameArg);
        command.AddOption(workspaceOption);
        command.AddOption(rawOption);

        command.SetHandler(async (snippetId, fileName, workspace, raw) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials?.AccessToken is null && credentials?.AppPassword is null)
            {
                Console.Error.WriteLine("Error: Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            workspace ??= credentials.DefaultWorkspace;
            if (string.IsNullOrEmpty(workspace))
            {
                Console.Error.WriteLine("Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    // List files in snippet
                    var snippet = await client.GetAsync<JsonElement>($"snippets/{workspace}/{snippetId}/files");

                    var files = new List<object>();
                    if (snippet.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var file in snippet.EnumerateArray())
                        {
                            files.Add(new
                            {
                                path = file.TryGetProperty("path", out var p) ? p.GetString() : null,
                                size = file.TryGetProperty("size", out var s) ? s.GetInt64() : 0,
                                mimetype = file.TryGetProperty("mimetype", out var m) ? m.GetString() : null
                            });
                        }
                    }

                    Console.WriteLine(JsonSerializer.Serialize(new { files, count = files.Count },
                        new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    // Get specific file content
                    var content = await client.GetRawAsync($"snippets/{workspace}/{snippetId}/files/{fileName}");

                    if (raw)
                    {
                        Console.Write(content);
                    }
                    else
                    {
                        Console.WriteLine(JsonSerializer.Serialize(new
                        {
                            file_name = fileName,
                            content
                        }, new JsonSerializerOptions { WriteIndented = true }));
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, snippetIdArg, fileNameArg, workspaceOption, rawOption);

        return command;
    }

    private static Command CreateWatchCommand()
    {
        var command = new Command("watch", "Watch/unwatch a snippet or list watchers");

        var snippetIdArg = new Argument<string>("snippet-id", "Snippet ID");

        var workspaceOption = new Option<string?>(
            ["--workspace", "-w"],
            "Workspace slug (uses default if not specified)");

        var listOption = new Option<bool>(
            ["--list", "-l"],
            () => false,
            "List watchers instead of watching");

        var unwatchOption = new Option<bool>(
            ["--unwatch", "-u"],
            () => false,
            "Stop watching the snippet");

        command.AddArgument(snippetIdArg);
        command.AddOption(workspaceOption);
        command.AddOption(listOption);
        command.AddOption(unwatchOption);

        command.SetHandler(async (snippetId, workspace, list, unwatch) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials?.AccessToken is null && credentials?.AppPassword is null)
            {
                Console.Error.WriteLine("Error: Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            workspace ??= credentials.DefaultWorkspace;
            if (string.IsNullOrEmpty(workspace))
            {
                Console.Error.WriteLine("Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                if (list)
                {
                    var watchers = new List<object>();
                    await foreach (var watcher in client.GetPaginatedAsync<JsonElement>($"snippets/{workspace}/{snippetId}/watchers"))
                    {
                        watchers.Add(new
                        {
                            display_name = watcher.TryGetProperty("display_name", out var d) ? d.GetString() : null,
                            username = watcher.TryGetProperty("username", out var u) ? u.GetString() : null,
                            account_id = watcher.TryGetProperty("account_id", out var a) ? a.GetString() : null
                        });
                    }

                    Console.WriteLine(JsonSerializer.Serialize(new { watchers, count = watchers.Count },
                        new JsonSerializerOptions { WriteIndented = true }));
                }
                else if (unwatch)
                {
                    await client.DeleteAsync($"snippets/{workspace}/{snippetId}/watch");
                    Console.WriteLine(JsonSerializer.Serialize(new { watching = false, snippet_id = snippetId },
                        new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    await client.PutAsync<JsonElement>($"snippets/{workspace}/{snippetId}/watch", new { });
                    Console.WriteLine(JsonSerializer.Serialize(new { watching = true, snippet_id = snippetId },
                        new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, snippetIdArg, workspaceOption, listOption, unwatchOption);

        return command;
    }

    private static Command CreateCommentsCommand()
    {
        var command = new Command("comments", "Manage snippet comments");

        var snippetIdArg = new Argument<string>("snippet-id", "Snippet ID");

        var workspaceOption = new Option<string?>(
            ["--workspace", "-w"],
            "Workspace slug (uses default if not specified)");

        var addOption = new Option<string?>(
            ["--add", "-a"],
            "Add a new comment with this content");

        var deleteOption = new Option<int?>(
            ["--delete", "-d"],
            "Delete comment by ID");

        command.AddArgument(snippetIdArg);
        command.AddOption(workspaceOption);
        command.AddOption(addOption);
        command.AddOption(deleteOption);

        command.SetHandler(async (snippetId, workspace, addContent, deleteId) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials?.AccessToken is null && credentials?.AppPassword is null)
            {
                Console.Error.WriteLine("Error: Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            workspace ??= credentials.DefaultWorkspace;
            if (string.IsNullOrEmpty(workspace))
            {
                Console.Error.WriteLine("Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                if (!string.IsNullOrEmpty(addContent))
                {
                    var comment = await client.PostAsync<JsonElement>(
                        $"snippets/{workspace}/{snippetId}/comments",
                        new { content = new { raw = addContent } });

                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        id = comment.GetProperty("id").GetInt32(),
                        content = comment.TryGetProperty("content", out var c) && c.TryGetProperty("raw", out var r) ? r.GetString() : null,
                        created_on = comment.TryGetProperty("created_on", out var co) ? co.GetString() : null
                    }, new JsonSerializerOptions { WriteIndented = true }));
                }
                else if (deleteId.HasValue)
                {
                    await client.DeleteAsync($"snippets/{workspace}/{snippetId}/comments/{deleteId}");
                    Console.WriteLine(JsonSerializer.Serialize(new { deleted = true, comment_id = deleteId },
                        new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    // List comments
                    var comments = new List<object>();
                    await foreach (var comment in client.GetPaginatedAsync<JsonElement>($"snippets/{workspace}/{snippetId}/comments"))
                    {
                        comments.Add(new
                        {
                            id = comment.GetProperty("id").GetInt32(),
                            content = comment.TryGetProperty("content", out var c) && c.TryGetProperty("raw", out var r) ? r.GetString() : null,
                            user = comment.TryGetProperty("user", out var u) && u.TryGetProperty("display_name", out var d) ? d.GetString() : null,
                            created_on = comment.TryGetProperty("created_on", out var co) ? co.GetString() : null
                        });
                    }

                    Console.WriteLine(JsonSerializer.Serialize(new { comments, count = comments.Count },
                        new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, snippetIdArg, workspaceOption, addOption, deleteOption);

        return command;
    }
}
