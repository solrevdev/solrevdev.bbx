using System.CommandLine;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Commands;

public static class WorkspaceCommand
{
    public static Command Create()
    {
        var command = new Command("workspace", "Manage Bitbucket workspaces");

        command.AddCommand(CreateListCommand());
        command.AddCommand(CreateViewCommand());
        command.AddCommand(CreateMembersCommand());
        command.AddCommand(CreateProjectsCommand());
        command.AddCommand(CreatePermissionsCommand());
        command.AddCommand(CreateHooksCommand());

        return command;
    }

    private static Command CreateListCommand()
    {
        var command = new Command("list", "List workspaces the user belongs to");

        var roleOption = new Option<string?>(
            ["--role", "-r"],
            "Filter by role: owner, collaborator, member");

        var limitOption = new Option<int>(
            ["--limit", "-l"],
            () => 25,
            "Maximum number of workspaces to return");

        command.AddOption(roleOption);
        command.AddOption(limitOption);

        command.SetHandler(async (role, limit) =>
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
                var endpoint = "workspaces";
                if (!string.IsNullOrEmpty(role))
                {
                    endpoint += $"?role={role}";
                }

                var workspaces = new List<object>();
                await foreach (var ws in client.GetPaginatedAsync<JsonElement>(endpoint))
                {
                    workspaces.Add(new
                    {
                        slug = ws.TryGetProperty("slug", out var s) ? s.GetString() : null,
                        name = ws.TryGetProperty("name", out var n) ? n.GetString() : null,
                        uuid = ws.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                        is_private = ws.TryGetProperty("is_private", out var p) && p.GetBoolean(),
                        created_on = ws.TryGetProperty("created_on", out var c) ? c.GetString() : null
                    });

                    if (workspaces.Count >= limit) break;
                }

                Console.WriteLine(JsonSerializer.Serialize(new { workspaces, count = workspaces.Count },
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, roleOption, limitOption);

        return command;
    }

    private static Command CreateViewCommand()
    {
        var command = new Command("view", "View workspace details");

        var workspaceArg = new Argument<string?>("workspace", () => null, "Workspace slug (uses default if not specified)");

        command.AddArgument(workspaceArg);

        command.SetHandler(async (workspace) =>
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
                Console.Error.WriteLine("Error: Workspace required. Provide as argument or set default with 'bbx auth set-workspace'.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                var ws = await client.GetAsync<JsonElement>($"workspaces/{workspace}");

                var result = new
                {
                    slug = ws.TryGetProperty("slug", out var s) ? s.GetString() : null,
                    name = ws.TryGetProperty("name", out var n) ? n.GetString() : null,
                    uuid = ws.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                    is_private = ws.TryGetProperty("is_private", out var p) && p.GetBoolean(),
                    created_on = ws.TryGetProperty("created_on", out var c) ? c.GetString() : null,
                    links = ws.TryGetProperty("links", out var l) ? new
                    {
                        html = l.TryGetObject("html", out var h) && h.TryGetProperty("href", out var href) ? href.GetString() : null,
                        avatar = l.TryGetObject("avatar", out var a) && a.TryGetProperty("href", out var ahref) ? ahref.GetString() : null
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
        }, workspaceArg);

        return command;
    }

    private static Command CreateMembersCommand()
    {
        var command = new Command("members", "List workspace members");

        var workspaceOption = new Option<string?>(
            ["--workspace", "-w"],
            "Workspace slug (uses default if not specified)");

        var limitOption = new Option<int>(
            ["--limit", "-l"],
            () => 50,
            "Maximum number of members to return");

        command.AddOption(workspaceOption);
        command.AddOption(limitOption);

        command.SetHandler(async (workspace, limit) =>
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
                var members = new List<object>();
                await foreach (var member in client.GetPaginatedAsync<JsonElement>($"workspaces/{workspace}/members"))
                {
                    var user = member.TryGetProperty("user", out var u) ? u : member;

                    members.Add(new
                    {
                        display_name = user.TryGetProperty("display_name", out var d) ? d.GetString() : null,
                        username = user.TryGetProperty("username", out var un) ? un.GetString() : null,
                        account_id = user.TryGetProperty("account_id", out var a) ? a.GetString() : null,
                        uuid = user.TryGetProperty("uuid", out var uuid) ? uuid.GetString() : null
                    });

                    if (members.Count >= limit) break;
                }

                Console.WriteLine(JsonSerializer.Serialize(new { members, count = members.Count },
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, limitOption);

        return command;
    }

    private static Command CreateProjectsCommand()
    {
        var command = new Command("projects", "Manage workspace projects");

        var workspaceOption = new Option<string?>(
            ["--workspace", "-w"],
            "Workspace slug (uses default if not specified)");

        var viewOption = new Option<string?>(
            ["--view", "-v"],
            "View specific project by key");

        var createOption = new Option<string?>(
            ["--create", "-c"],
            "Create new project with this name");

        var keyOption = new Option<string?>(
            ["--key", "-k"],
            "Project key (required for create, used for operations)");

        var descriptionOption = new Option<string?>(
            ["--description", "-d"],
            "Project description (for create)");

        var privateOption = new Option<bool?>(
            ["--private", "-p"],
            "Make project private (for create)");

        var deleteOption = new Option<bool>(
            ["--delete"],
            () => false,
            "Delete the project specified by --key");

        var yesOption = new Option<bool>(
            ["--yes", "-y"],
            () => false,
            "Skip confirmation prompt for delete");

        var limitOption = new Option<int>(
            ["--limit", "-l"],
            () => 25,
            "Maximum number of projects to return");

        command.AddOption(workspaceOption);
        command.AddOption(viewOption);
        command.AddOption(createOption);
        command.AddOption(keyOption);
        command.AddOption(descriptionOption);
        command.AddOption(privateOption);
        command.AddOption(deleteOption);
        command.AddOption(yesOption);
        command.AddOption(limitOption);

        command.SetHandler(async (context) =>
        {
            var workspace = context.ParseResult.GetValueForOption(workspaceOption);
            var view = context.ParseResult.GetValueForOption(viewOption);
            var create = context.ParseResult.GetValueForOption(createOption);
            var key = context.ParseResult.GetValueForOption(keyOption);
            var description = context.ParseResult.GetValueForOption(descriptionOption);
            var isPrivate = context.ParseResult.GetValueForOption(privateOption);
            var delete = context.ParseResult.GetValueForOption(deleteOption);
            var yes = context.ParseResult.GetValueForOption(yesOption);
            var limit = context.ParseResult.GetValueForOption(limitOption);

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
                if (!string.IsNullOrEmpty(view))
                {
                    // View specific project
                    var project = await client.GetAsync<JsonElement>($"workspaces/{workspace}/projects/{view}");

                    var result = new
                    {
                        key = project.TryGetProperty("key", out var k) ? k.GetString() : null,
                        name = project.TryGetProperty("name", out var n) ? n.GetString() : null,
                        description = project.TryGetProperty("description", out var d) ? d.GetString() : null,
                        uuid = project.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                        is_private = project.TryGetProperty("is_private", out var p) && p.GetBoolean(),
                        created_on = project.TryGetProperty("created_on", out var c) ? c.GetString() : null,
                        updated_on = project.TryGetProperty("updated_on", out var up) ? up.GetString() : null
                    };

                    Console.WriteLine(JsonSerializer.Serialize(result,
                        new JsonSerializerOptions { WriteIndented = true }));
                }
                else if (!string.IsNullOrEmpty(create))
                {
                    // Create new project
                    if (string.IsNullOrEmpty(key))
                    {
                        Console.Error.WriteLine("Error: --key is required when creating a project.");
                        Environment.ExitCode = 1;
                        return;
                    }

                    var payload = new Dictionary<string, object>
                    {
                        ["name"] = create,
                        ["key"] = key,
                        ["is_private"] = isPrivate ?? true
                    };

                    if (!string.IsNullOrEmpty(description))
                    {
                        payload["description"] = description;
                    }

                    var project = await client.PostAsync<JsonElement>($"workspaces/{workspace}/projects", payload);

                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        key = project.TryGetProperty("key", out var k) ? k.GetString() : null,
                        name = project.TryGetProperty("name", out var n) ? n.GetString() : null,
                        created_on = project.TryGetProperty("created_on", out var c) ? c.GetString() : null
                    }, new JsonSerializerOptions { WriteIndented = true }));
                }
                else if (delete)
                {
                    // Delete project
                    if (string.IsNullOrEmpty(key))
                    {
                        Console.Error.WriteLine("Error: --key is required when deleting a project.");
                        Environment.ExitCode = 1;
                        return;
                    }

                    if (!yes)
                    {
                        Console.Error.Write($"Delete project '{key}'? [y/N]: ");
                        var response = Console.ReadLine()?.Trim().ToLower();
                        if (response != "y" && response != "yes")
                        {
                            Console.Error.WriteLine("Cancelled.");
                            return;
                        }
                    }

                    await client.DeleteAsync($"workspaces/{workspace}/projects/{key}");
                    Console.WriteLine(JsonSerializer.Serialize(new { deleted = true, project_key = key },
                        new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    // List projects
                    var projects = new List<object>();
                    await foreach (var project in client.GetPaginatedAsync<JsonElement>($"workspaces/{workspace}/projects"))
                    {
                        projects.Add(new
                        {
                            key = project.TryGetProperty("key", out var k) ? k.GetString() : null,
                            name = project.TryGetProperty("name", out var n) ? n.GetString() : null,
                            description = project.TryGetProperty("description", out var d) ? d.GetString() : null,
                            is_private = project.TryGetProperty("is_private", out var p) && p.GetBoolean(),
                            created_on = project.TryGetProperty("created_on", out var c) ? c.GetString() : null
                        });

                        if (projects.Count >= limit) break;
                    }

                    Console.WriteLine(JsonSerializer.Serialize(new { projects, count = projects.Count },
                        new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        });

        return command;
    }

    private static Command CreatePermissionsCommand()
    {
        var command = new Command("permissions", "View workspace permissions");

        var workspaceOption = new Option<string?>(
            ["--workspace", "-w"],
            "Workspace slug (uses default if not specified)");

        var limitOption = new Option<int>(
            ["--limit", "-l"],
            () => 50,
            "Maximum number of permissions to return");

        command.AddOption(workspaceOption);
        command.AddOption(limitOption);

        command.SetHandler(async (workspace, limit) =>
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
                var permissions = new List<object>();
                await foreach (var perm in client.GetPaginatedAsync<JsonElement>($"workspaces/{workspace}/permissions"))
                {
                    permissions.Add(new
                    {
                        permission = perm.TryGetProperty("permission", out var p) ? p.GetString() : null,
                        user = perm.TryGetProperty("user", out var u) ? new
                        {
                            display_name = u.TryGetProperty("display_name", out var d) ? d.GetString() : null,
                            username = u.TryGetProperty("username", out var un) ? un.GetString() : null,
                            account_id = u.TryGetProperty("account_id", out var a) ? a.GetString() : null
                        } : null,
                        workspace = perm.TryGetObject("workspace", out var w) && w.TryGetProperty("slug", out var s) ? s.GetString() : null
                    });

                    if (permissions.Count >= limit) break;
                }

                Console.WriteLine(JsonSerializer.Serialize(new { permissions, count = permissions.Count },
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, limitOption);

        return command;
    }

    private static Command CreateHooksCommand()
    {
        var command = new Command("hooks", "Manage workspace webhooks");

        var workspaceOption = new Option<string?>(
            ["--workspace", "-w"],
            "Workspace slug (uses default if not specified)");

        var viewOption = new Option<string?>(
            ["--view", "-v"],
            "View specific webhook by UUID");

        var createOption = new Option<string?>(
            ["--create", "-c"],
            "Create webhook with this URL");

        var descriptionOption = new Option<string?>(
            ["--description", "-d"],
            "Webhook description");

        var eventsOption = new Option<string[]?>(
            ["--events", "-e"],
            "Events to trigger webhook (e.g., repo:push, pullrequest:created)");

        var activeOption = new Option<bool?>(
            ["--active", "-a"],
            "Whether webhook is active");

        var deleteOption = new Option<string?>(
            ["--delete"],
            "Delete webhook by UUID");

        var yesOption = new Option<bool>(
            ["--yes", "-y"],
            () => false,
            "Skip confirmation prompt for delete");

        var limitOption = new Option<int>(
            ["--limit", "-l"],
            () => 25,
            "Maximum number of webhooks to return");

        command.AddOption(workspaceOption);
        command.AddOption(viewOption);
        command.AddOption(createOption);
        command.AddOption(descriptionOption);
        command.AddOption(eventsOption);
        command.AddOption(activeOption);
        command.AddOption(deleteOption);
        command.AddOption(yesOption);
        command.AddOption(limitOption);

        command.SetHandler(async (context) =>
        {
            var workspace = context.ParseResult.GetValueForOption(workspaceOption);
            var view = context.ParseResult.GetValueForOption(viewOption);
            var create = context.ParseResult.GetValueForOption(createOption);
            var description = context.ParseResult.GetValueForOption(descriptionOption);
            var events = context.ParseResult.GetValueForOption(eventsOption);
            var active = context.ParseResult.GetValueForOption(activeOption);
            var deleteUuid = context.ParseResult.GetValueForOption(deleteOption);
            var yes = context.ParseResult.GetValueForOption(yesOption);
            var limit = context.ParseResult.GetValueForOption(limitOption);

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
                if (!string.IsNullOrEmpty(view))
                {
                    // View specific webhook
                    var hook = await client.GetAsync<JsonElement>($"workspaces/{workspace}/hooks/{view}");

                    var hookEvents = new List<string>();
                    if (hook.TryGetProperty("events", out var e) && e.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var ev in e.EnumerateArray())
                        {
                            if (ev.GetString() is string evStr)
                                hookEvents.Add(evStr);
                        }
                    }

                    var result = new
                    {
                        uuid = hook.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                        description = hook.TryGetProperty("description", out var d) ? d.GetString() : null,
                        url = hook.TryGetProperty("url", out var url) ? url.GetString() : null,
                        active = hook.TryGetProperty("active", out var a) && a.GetBoolean(),
                        events = hookEvents,
                        created_at = hook.TryGetProperty("created_at", out var c) ? c.GetString() : null
                    };

                    Console.WriteLine(JsonSerializer.Serialize(result,
                        new JsonSerializerOptions { WriteIndented = true }));
                }
                else if (!string.IsNullOrEmpty(create))
                {
                    // Create webhook
                    var payload = new Dictionary<string, object>
                    {
                        ["url"] = create,
                        ["active"] = active ?? true,
                        ["events"] = events ?? new[] { "repo:push" }
                    };

                    if (!string.IsNullOrEmpty(description))
                    {
                        payload["description"] = description;
                    }

                    var hook = await client.PostAsync<JsonElement>($"workspaces/{workspace}/hooks", payload);

                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        uuid = hook.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                        url = hook.TryGetProperty("url", out var url) ? url.GetString() : null,
                        active = hook.TryGetProperty("active", out var a) && a.GetBoolean(),
                        created_at = hook.TryGetProperty("created_at", out var c) ? c.GetString() : null
                    }, new JsonSerializerOptions { WriteIndented = true }));
                }
                else if (!string.IsNullOrEmpty(deleteUuid))
                {
                    // Delete webhook
                    if (!yes)
                    {
                        Console.Error.Write($"Delete webhook '{deleteUuid}'? [y/N]: ");
                        var response = Console.ReadLine()?.Trim().ToLower();
                        if (response != "y" && response != "yes")
                        {
                            Console.Error.WriteLine("Cancelled.");
                            return;
                        }
                    }

                    await client.DeleteAsync($"workspaces/{workspace}/hooks/{deleteUuid}");
                    Console.WriteLine(JsonSerializer.Serialize(new { deleted = true, uuid = deleteUuid },
                        new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    // List webhooks
                    var hooks = new List<object>();
                    await foreach (var hook in client.GetPaginatedAsync<JsonElement>($"workspaces/{workspace}/hooks"))
                    {
                        var hookEvents = new List<string>();
                        if (hook.TryGetProperty("events", out var ev) && ev.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var evt in ev.EnumerateArray())
                            {
                                if (evt.GetString() is string evStr)
                                    hookEvents.Add(evStr);
                            }
                        }

                        hooks.Add(new
                        {
                            uuid = hook.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                            description = hook.TryGetProperty("description", out var d) ? d.GetString() : null,
                            url = hook.TryGetProperty("url", out var url) ? url.GetString() : null,
                            active = hook.TryGetProperty("active", out var a) && a.GetBoolean(),
                            events = hookEvents
                        });

                        if (hooks.Count >= limit) break;
                    }

                    Console.WriteLine(JsonSerializer.Serialize(new { hooks, count = hooks.Count },
                        new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        });

        return command;
    }
}
