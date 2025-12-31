# CLAUDE.md

This file provides context for Claude Code when working with the solrevdev.bbx project.

## Project Overview

`solrevdev.bbx` is a .NET global tool that provides CLI access to Bitbucket Cloud API v2. It's designed for LLM integration with all output in JSON format.

## Build Commands

```bash
# Build the project
dotnet build src/Bbx/Bbx.csproj

# Run directly
dotnet run --project src/Bbx/Bbx.csproj -- <command>

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
│   ├── Program.cs              # Entry point, command routing
│   ├── Api/
│   │   └── BitbucketClient.cs  # HTTP client with pagination
│   ├── Auth/
│   │   └── CredentialManager.cs # Secure credential storage
│   └── Commands/
│       ├── AuthCommand.cs      # login, logout, status, token, set-workspace
│       ├── RepoCommand.cs      # list, view, create, delete, fork, clone-url
│       ├── PrCommand.cs        # list, view, create, merge, approve, comments
│       ├── BranchCommand.cs    # list, view, create, delete, restrictions
│       ├── CommitCommand.cs    # list, view, diff, patch, statuses
│       ├── IssueCommand.cs     # list, view, create, update, delete, comments
│       ├── PipelineCommand.cs  # list, view, trigger, stop, logs, deployments
│       ├── SnippetCommand.cs   # list, view, create, update, delete, files
│       └── WorkspaceCommand.cs # list, view, members, projects, permissions
├── .github/workflows/
│   └── publish.yml             # CI/CD with auto version bumping
├── README.md
├── CLAUDE.md
└── LICENSE
```

## Key Design Decisions

1. **JSON-only output** - All commands output JSON for LLM consumption
2. **Error handling** - Errors go to stderr, data to stdout
3. **Authentication** - App passwords stored in `~/.config/bbx/config.json`
4. **Pagination** - Transparent pagination via IAsyncEnumerable
5. **Confirmation prompts** - Destructive operations require `--yes` to skip

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
            Console.Error.WriteLine("Error: Not authenticated. Run 'bbx auth login' first.");
            Environment.ExitCode = 1;
            return;
        }
        
        var client = new BitbucketClient(credentials);
        
        try
        {
            var result = await client.GetAsync<JsonElement>("endpoint");
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
await foreach (var item in client.GetPaginatedAsync<JsonElement>("endpoint"))
{
    items.Add(new { /* shape data */ });
    if (items.Count >= limit) break;
}
Console.WriteLine(JsonSerializer.Serialize(new { items, count = items.Count }));
```

## API Reference

Base URL: `https://api.bitbucket.org/2.0`

Common endpoints:
- `repositories/{workspace}` - List repos
- `repositories/{workspace}/{repo}/pullrequests` - Pull requests
- `repositories/{workspace}/{repo}/refs/branches` - Branches
- `repositories/{workspace}/{repo}/commits` - Commits
- `repositories/{workspace}/{repo}/issues` - Issues
- `repositories/{workspace}/{repo}/pipelines` - Pipelines
- `snippets/{workspace}` - Snippets
- `workspaces` - Workspaces

## Testing

To test commands against live Bitbucket API:

```bash
# Set up auth first
bbx auth login --app-password
bbx auth set-workspace yourworkspace

# Test individual commands
bbx repo list --limit 5
bbx pr list --repo yourrepo --limit 5
bbx workspace list
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
