# CLAUDE.md

This file provides context for Claude Code when working with the solrevdev.bbx project.

## Project Overview

`solrevdev.bbx` is a .NET global tool that provides CLI access to Bitbucket Cloud API v2. It's designed for LLM integration with all output in JSON format.

## Build Commands

```bash
# Build the project
dotnet build src/Bbx/Bbx.csproj

# Run tests (xUnit, net10.0 single-target)
dotnet test tests/Bbx.Tests/Bbx.Tests.csproj

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
│   ├── Program.cs              # Entry point + static Services provider (built once in Main)
│   ├── BbxUserException.cs     # User-facing error type (caught by CommandRunner)
│   ├── Api/
│   │   └── BitbucketClient.cs  # HTTP client with pagination, endpoint normalization
│   ├── Auth/
│   │   ├── CredentialManager.cs # ~/.config/bbx/config.json + legacy AppPassword migration
│   │   ├── ICredentialStore.cs  # Seam for tests (FileCredentialStore in prod)
│   │   ├── FileCredentialStore.cs
│   │   ├── IAuthProvider.cs     # Per-request auth header stamping
│   │   ├── BasicAuthProvider.cs # email:api-token (or username:app-password)
│   │   └── NullAuthProvider.cs  # public endpoints / pre-login
│   ├── Composition/
│   │   ├── ServiceRegistration.cs # DI container build (singleton HttpClient + handler graph)
│   │   └── JsonOptions.cs       # Shared System.Text.Json options (snake_case, indented)
│   ├── Features/                # Per-verb feature slices — request + handler co-located
│   │   ├── Auth/{LoginApiToken,LoginAppPassword,LoginGuide,Status,Logout,Token,SetWorkspace}/
│   │   ├── Repos/{ListRepos,ViewRepo,CreateRepo,DeleteRepo,ForkRepo,CloneRepo,RepoPermissions}/
│   │   ├── PullRequests/{ListPullRequests,...,PullRequestStatuses}/
│   │   ├── Branches/{ListBranches,...,DeleteBranchRestriction}/
│   │   ├── Commits/{ListCommits,...,ListCommitPullRequests}/
│   │   ├── Issues/{ListIssues,...,AddIssueComment}/
│   │   ├── Pipelines/{ListPipelines,...,ViewDeploymentEnvironment}/
│   │   ├── Snippets/{ListSnippets,...,SnippetComments}/
│   │   ├── Workspaces/{ListWorkspaces,...,WorkspaceHooks}/
│   │   └── Common/Resolve.cs    # workspace/repo defaulting helpers
│   └── Commands/                # System.CommandLine wiring only — NO business logic
│       ├── CommandRunner.cs     # try/catch helper around handler invocation
│       ├── CommandOptions.cs    # Shared workspace/repo options
│       ├── AuthCommand.cs       # login (--api-token, --app-password), logout, status, token, set-workspace
│       ├── RepoCommand.cs       # list, view, create, delete, fork, clone, permissions
│       ├── PrCommand.cs         # list, view, create, merge, approve, unapprove, decline, comments, comment, diff, activity, statuses
│       ├── BranchCommand.cs     # list, view, create, delete, restrictions
│       ├── CommitCommand.cs     # list, view, diff, patch, comments, statuses, pullrequests
│       ├── IssueCommand.cs      # list, view, create, update, delete, comments, comment
│       ├── PipelineCommand.cs   # list, view, trigger, stop, logs, steps, variables, schedules, caches, deployments
│       ├── SnippetCommand.cs    # list, view, create, update, delete, files, watch, comments
│       └── WorkspaceCommand.cs  # list, view, members, projects, permissions, hooks
├── tests/Bbx.Tests/             # xUnit + FluentAssertions + NSubstitute (net10.0)
│   ├── Bbx.Tests.csproj
│   ├── TestKit/                 # FakeHttpMessageHandler, InMemoryCredentialStore
│   ├── Api/BitbucketClientTests.cs
│   ├── Auth/{CredentialManagerTests,BasicAuthProviderTests,NullAuthProviderTests}.cs
│   └── Features/Repos/ListReposHandlerTests.cs  # template for per-handler tests
├── .github/workflows/
│   └── publish.yml              # dotnet test runs before pack
├── README.md
├── CLAUDE.md
└── LICENSE
```

Handlers resolve from `Program.Services` (a `static IServiceProvider` built once
in `Main` via `Composition.ServiceRegistration.Build()`). Each `Commands/*.cs`
`SetHandler` does only: parse options → `services.GetRequiredService<Handler>()`
→ `handler.HandleAsync(new Request(...), CancellationToken.None)` → serialise
via `CommandRunner.RunJsonAsync` / `RunRawAsync` / `RunActionAsync`. Anything
that calls Bitbucket or shapes JSON lives in `Features/<Group>/<Verb>/`.

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

### Adding a new subcommand (Phase 0.5+ shape)

Three files per verb, no exceptions:

1. `src/Bbx/Features/<Group>/<Verb>/<Verb>Request.cs` — `public sealed record` of inputs.
2. `src/Bbx/Features/<Group>/<Verb>/<Verb>Handler.cs` — `public sealed class` with a
   primary constructor taking `BitbucketClient` (plus `CredentialManager` when it
   needs `DefaultWorkspace` or persistence). Single `HandleAsync` per handler.
3. `src/Bbx/Commands/<Group>Command.cs` — wiring only: parse System.CommandLine
   options, resolve the handler from `Program.Services`, await it through
   `CommandRunner`.

Then add one `services.AddTransient<TheNewHandler>();` line in
`Composition/ServiceRegistration.cs`. No reflection scanning — explicit registrations.

```csharp
// src/Bbx/Features/Repos/ListRepos/ListReposRequest.cs
public sealed record ListReposRequest(string? Workspace, int Limit, string? Query);

// src/Bbx/Features/Repos/ListRepos/ListReposHandler.cs
public sealed class ListReposHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListReposRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        // Endpoints may begin with or without "/"; BitbucketClient.NormalizeEndpoint handles it.
        var endpoint = $"/repositories/{workspace}";
        var repos = new List<object>();
        await foreach (var repo in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            repos.Add(new { /* shape data */ });
            if (repos.Count >= request.Limit) break;
        }
        return new { workspace, count = repos.Count, repositories = repos };
    }
}

// src/Bbx/Commands/RepoCommand.cs
listCommand.SetHandler((string? workspace, int limit, string? query) =>
    CommandRunner.RunJsonAsync(() =>
        services.GetRequiredService<ListReposHandler>()
            .HandleAsync(new ListReposRequest(workspace, limit, query), CancellationToken.None)),
    workspaceOption, limitOption, queryOption);
```

Handlers throw `BbxUserException` for user-facing errors (missing workspace,
"not authenticated", etc.) — `CommandRunner.Run*Async` catches it, writes the
verbatim message to stderr, and sets `Environment.ExitCode = 1`. Unhandled
`HttpRequestException` from Bitbucket comes through prefixed with `"Error: "`.

### Paginated responses

```csharp
var items = new List<object>();
await foreach (var item in client.GetPaginatedAsync<JsonElement>("repositories/workspace/repo/pullrequests", ct))
{
    items.Add(new { /* shape data */ });
    if (items.Count >= limit) break;
}
return new { items, count = items.Count };
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

- `System.CommandLine` — CLI framework
- `Microsoft.Extensions.DependencyInjection` — sole composition root for the
  `BitbucketClient` + `CredentialManager` + handler graph
- Otherwise built-in `System.Text.Json` for serialisation
- Test project adds: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`,
  `FluentAssertions`, `NSubstitute`

## .NET Version Support

Multi-targets .NET 8, 9, and 10:
- **.NET 10** - Latest LTS (recommended)
- **.NET 9** - Current
- **.NET 8** - Previous LTS
