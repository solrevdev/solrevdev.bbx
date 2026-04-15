# CLAUDE.md

This file provides context for Claude Code when working with the solrevdev.bbx project.

## Project Overview

`solrevdev.bbx` is a .NET global tool that provides CLI access to Bitbucket Cloud API v2. It's designed for LLM integration with all output in JSON format.

## Build Commands

```bash
# Build the project
dotnet build src/Bbx/Bbx.csproj

# Run directly (use -f to target a specific framework)
dotnet run --project src/Bbx/Bbx.csproj -f net10.0 -- <command>

# Pack for distribution
dotnet pack src/Bbx/Bbx.csproj -c Release

# Install locally for testing
dotnet tool install -g --add-source ./src/Bbx/bin/Release solrevdev.bbx

# Uninstall
dotnet tool uninstall -g solrevdev.bbx
```

## Project Structure

```
solrevdev.bbx/
├── src/Bbx/
│   ├── Bbx.csproj              # Multi-target: net8.0;net9.0;net10.0 (LTS: 8, 10)
│   ├── Program.cs              # Entry point, command routing, global -w/-r options
│   ├── Api/
│   │   └── BitbucketClient.cs  # HTTP client with pagination, endpoint normalization
│   ├── Auth/
│   │   └── CredentialManager.cs # Secure credential storage (~/.config/bbx/config.json)
│   └── Commands/
│       ├── AuthCommand.cs      # login (--api-token, --app-password), logout, status, token, set-workspace
│       ├── RepoCommand.cs      # list, view, create, delete, fork, clone, permissions
│       ├── PrCommand.cs        # list, view, create, merge, approve, unapprove, decline, comments, comment, diff, activity, statuses
│       ├── BranchCommand.cs    # list, view, create, delete, restrictions
│       ├── CommitCommand.cs    # list, view, diff, patch, comments, statuses, pullrequests
│       ├── IssueCommand.cs     # list, view, create, update, delete, comments, comment
│       ├── PipelineCommand.cs  # list, view, trigger, stop, logs, steps, variables, schedules, caches, deployments
│       ├── SnippetCommand.cs   # list, view, create, update, delete, files, watch, comments
│       └── WorkspaceCommand.cs # list, view, members, projects, permissions, hooks
├── .github/workflows/
│   └── publish.yml             # CI/CD with auto version bumping
├── README.md
├── CLAUDE.md
└── LICENSE
```

## Command Syntax

The `-w`/`--workspace` and `-r`/`--repo` options are registered as global options on the
`repo`, `pr`, `branch`, `commit`, and `issue` command groups, so they appear on the
subcommand help that agents will actually use:

```bash
# Help-aligned form
bbx pr list -w myworkspace -r myrepo --state OPEN
bbx branch view main -w myworkspace -r myrepo

# Still accepted
bbx pr -w myworkspace -r myrepo list --state OPEN
```

Prefer the help-aligned form in docs, tests, and agent examples so command lines match
the binary's `--help` output.

## Key Design Decisions

1. **JSON-only output** - All commands output JSON for LLM consumption
2. **Error handling** - Errors go to stderr, data to stdout
3. **Authentication** - API tokens (or legacy app passwords) stored in `~/.config/bbx/config.json`
4. **API token auth** - Uses Basic auth (email:token), same mechanism as app passwords. Atlassian API tokens are NOT Bearer tokens.
5. **Non-interactive auth** - `ReadPassword()` falls back to `Console.ReadLine()` when stdin is redirected so scripted login works
6. **Pagination** - Transparent pagination via IAsyncEnumerable
7. **Confirmation prompts** - Destructive operations require `--yes` to skip
8. **Endpoint normalization** - `BitbucketClient.NormalizeEndpoint()` strips leading `/` from endpoints to prevent HttpClient BaseAddress resolution bugs

## Common Patterns

### Adding a new subcommand

```csharp
private static Command CreateNewCommand()
{
    var command = new Command("name", "Description");

    // Add options
    var someOption = new Option<string>(["--opt", "-o"], "Description");
    command.AddOption(someOption);

    command.SetHandler(async (optValue) =>
    {
        var credentials = CredentialManager.Load();
        if (credentials?.AccessToken is null && credentials?.AppPassword is null)
        {
            Console.Error.WriteLine("Error: Not authenticated. Run 'bbx auth login --api-token' first.");
            Environment.ExitCode = 1;
            return;
        }

        var client = new BitbucketClient(credentials);

        try
        {
            // Endpoints should NOT have a leading slash
            var result = await client.GetAsync<JsonElement>("repositories/workspace/repo");
            Console.WriteLine(JsonSerializer.Serialize(result,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }, someOption);

    return command;
}
```

### Paginated responses

```csharp
var items = new List<object>();
await foreach (var item in client.GetPaginatedAsync<JsonElement>("repositories/workspace/repo/pullrequests"))
{
    items.Add(new { /* shape data */ });
    if (items.Count >= limit) break;
}
Console.WriteLine(JsonSerializer.Serialize(new { items, count = items.Count }));
```

## API Reference

Base URL: `https://api.bitbucket.org/2.0/`

Common endpoints (no leading slash):
- `repositories/{workspace}` - List repos
- `repositories/{workspace}/{repo}/pullrequests` - Pull requests
- `repositories/{workspace}/{repo}/refs/branches` - Branches
- `repositories/{workspace}/{repo}/commits` - Commits
- `repositories/{workspace}/{repo}/issues` - Issues
- `repositories/{workspace}/{repo}/pipelines` - Pipelines
- `snippets/{workspace}` - Snippets
- `workspaces` - Workspaces
- `user` - Current authenticated user

## Testing

To test commands against live Bitbucket API:

```bash
# Set up auth first (API token, not app password)
bbx auth login --api-token
bbx auth set-workspace yourworkspace

# Test individual commands
bbx repo list -w yourworkspace --limit 5
bbx pr list -w yourworkspace -r yourrepo --state OPEN --limit 5
bbx workspace list
```

When running from source without installing:

```bash
dotnet run --project src/Bbx/Bbx.csproj -f net10.0 -- auth login --api-token
dotnet run --project src/Bbx/Bbx.csproj -f net10.0 -- auth set-workspace foremost-group
dotnet run --project src/Bbx/Bbx.csproj -f net10.0 -- pr list -w foremost-group -r myrepo --state OPEN
```

## Publishing

The GitHub Actions workflow handles publishing:
1. Push to master triggers build (or manually via workflow_dispatch)
2. Version auto-bumped (defaults to patch; use workflow_dispatch to specify major/minor/patch)
3. Package pushed to NuGet
4. GitHub release created with auto-generated release notes

## Dependencies

- System.CommandLine (CLI framework)
- No other external dependencies - uses built-in System.Text.Json

## .NET Version Support

Multi-targets .NET 8, 9, and 10:
- **.NET 10** - Latest LTS (recommended)
- **.NET 9** - Current
- **.NET 8** - Previous LTS
