# solrevdev.bbx

[![NuGet Version](https://img.shields.io/nuget/v/solrevdev.bbx)](https://www.nuget.org/packages/solrevdev.bbx)
[![NuGet Downloads](https://img.shields.io/nuget/dt/solrevdev.bbx)](https://www.nuget.org/packages/solrevdev.bbx)

A .NET global tool providing Bitbucket Cloud API v2 access, designed for LLM integration. All output is JSON for easy parsing by AI assistants like Claude, GitHub Copilot, or custom workflows.

## Installation

```bash
dotnet tool install -g solrevdev.bbx
```

## Authentication

Before using `bbx`, authenticate with your Bitbucket account using an [API token](https://bitbucket.org/account/settings/api-tokens/):

```bash
# Login with API token (recommended)
bbx auth login --api-token

# Set default workspace
bbx auth set-workspace myworkspace

# Check authentication status
bbx auth status

# View stored token (for debugging)
bbx auth token

# Logout and clear credentials
bbx auth logout
```

> **Note:** App passwords were deprecated by Atlassian in September 2025 and will be
> disabled June 2026. Use `--api-token` instead. The `--app-password` flag still works
> for existing credentials but should not be used for new setups.

### Creating an API Token

1. Go to **Bitbucket Settings > API tokens** (<https://bitbucket.org/account/settings/api-tokens/>)
2. Create a new token with the scopes you need:

| Scope | Permission | For |
|-------|------------|-----|
| Account | Read | User info, auth status |
| Repositories | Read, Write, Admin | Repo management |
| Pull Requests | Read, Write | PR operations |
| Issues | Read, Write | Issue tracking |
| Pipelines | Read, Write | CI/CD |
| Snippets | Read, Write | Code snippets |

3. Run `bbx auth login --api-token`, enter your Atlassian account email and paste the token

## Command Syntax

The `-w`/`--workspace` and `-r`/`--repo` switches are exposed as global options on the
`repo`, `pr`, `branch`, `commit`, and `issue` command groups, so they show up directly
on subcommand help. The examples below use the help-aligned form with options after the
subcommand:

```bash
# Recommended
bbx pr list -w myworkspace -r myrepo --state OPEN

# Also accepted
bbx pr -w myworkspace -r myrepo list --state OPEN
```

If you set a default workspace with `bbx auth set-workspace`, you can omit `-w`:

```bash
bbx auth set-workspace myworkspace
bbx pr list -r myrepo --state OPEN
```

## Commands

### Repository Management

```bash
# List repositories in workspace
bbx repo list -w myworkspace

# View repository details
bbx repo view myrepo -w myworkspace

# Create a new repository
bbx repo create myrepo --private -w myworkspace

# Get clone URL
bbx repo clone myrepo -w myworkspace

# Fork a repository
bbx repo fork myrepo -w myworkspace

# Delete repository (requires --yes to confirm)
bbx repo delete myrepo --yes -w myworkspace

# View repository permissions
bbx repo permissions myrepo -w myworkspace
```

### Pull Requests

```bash
# List open PRs
bbx pr list -w myworkspace -r myrepo --state OPEN

# List PRs by author
bbx pr list -w myworkspace -r myrepo --author "{account-id}"

# View PR details
bbx pr view 123 -w myworkspace -r myrepo

# Create a new PR
bbx pr create -w myworkspace -r myrepo \
    --title "Add new feature" --source feature-branch --dest main \
    --body "Description here"

# Approve / unapprove PR
bbx pr approve 123 -w myworkspace -r myrepo
bbx pr unapprove 123 -w myworkspace -r myrepo

# Merge PR
bbx pr merge 123 -w myworkspace -r myrepo --strategy squash

# Decline PR
bbx pr decline 123 -w myworkspace -r myrepo

# View PR diff
bbx pr diff 123 -w myworkspace -r myrepo

# List PR comments
bbx pr comments 123 -w myworkspace -r myrepo

# Add a comment
bbx pr comment 123 -w myworkspace -r myrepo --body "LGTM!"

# View PR activity log
bbx pr activity 123 -w myworkspace -r myrepo

# View PR commit statuses (CI/CD)
bbx pr statuses 123 -w myworkspace -r myrepo
```

### Branch Management

```bash
# List branches
bbx branch list -w myworkspace -r myrepo

# Sort branches
bbx branch list -w myworkspace -r myrepo --sort -name

# View branch details
bbx branch view main -w myworkspace -r myrepo

# Create a branch
bbx branch create feature-x -w myworkspace -r myrepo --target main

# Delete a branch (requires --yes to confirm)
bbx branch delete feature-x -w myworkspace -r myrepo --yes

# List branch restrictions
bbx branch restrictions list -w myworkspace -r myrepo

# Add a branch restriction
bbx branch restrictions add -w myworkspace -r myrepo --kind push --pattern main
```

### Commits

```bash
# List recent commits
bbx commit list -w myworkspace -r myrepo --limit 20

# List commits on a branch
bbx commit list -w myworkspace -r myrepo --branch feature-x

# View commit details
bbx commit view abc123 -w myworkspace -r myrepo

# Get commit diff
bbx commit diff abc123 -w myworkspace -r myrepo

# Get commit as patch
bbx commit patch abc123 -w myworkspace -r myrepo

# List commit comments
bbx commit comments abc123 -w myworkspace -r myrepo

# View commit build statuses
bbx commit statuses abc123 -w myworkspace -r myrepo

# List pull requests for a commit
bbx commit pullrequests abc123 -w myworkspace -r myrepo
```

### Issues

```bash
# List issues
bbx issue list -w myworkspace -r myrepo

# Filter by state
bbx issue list -w myworkspace -r myrepo --state open

# View issue details
bbx issue view 42 -w myworkspace -r myrepo

# Create an issue
bbx issue create -w myworkspace -r myrepo \
    --title "Bug report" --content "Description..." \
    --priority major --kind bug

# Update issue
bbx issue update 42 -w myworkspace -r myrepo --state resolved

# Delete issue (requires --yes to confirm)
bbx issue delete 42 -w myworkspace -r myrepo --yes

# List issue comments
bbx issue comments 42 -w myworkspace -r myrepo

# Add a comment
bbx issue comment 42 -w myworkspace -r myrepo --body "Working on this"
```

### Pipelines

```bash
# List recent pipelines
bbx pipeline list -w myworkspace -r myrepo

# View pipeline details
bbx pipeline view {uuid} -w myworkspace -r myrepo

# Trigger a pipeline
bbx pipeline trigger -w myworkspace -r myrepo --branch main

# Stop a running pipeline
bbx pipeline stop {uuid} -w myworkspace -r myrepo

# View pipeline step logs
bbx pipeline logs {pipeline-uuid} {step-uuid} -w myworkspace -r myrepo

# List pipeline steps
bbx pipeline steps {uuid} -w myworkspace -r myrepo

# Manage pipeline variables
bbx pipeline variables list --workspace myworkspace --repo myrepo

# Manage schedules
bbx pipeline schedules list --workspace myworkspace --repo myrepo

# Manage caches
bbx pipeline caches list --workspace myworkspace --repo myrepo

# Manage deployment environments
bbx pipeline deployments list --workspace myworkspace --repo myrepo
```

### Snippets

```bash
# List snippets
bbx snippet list -w myworkspace

# View snippet
bbx snippet view abc123

# Create snippet from files
bbx snippet create --title "My snippet" --file script.sh --file config.json --private

# Update snippet
bbx snippet update abc123 --title "New title"

# Delete snippet
bbx snippet delete abc123

# List or get files in a snippet
bbx snippet files abc123
bbx snippet files abc123 script.sh

# Watch/unwatch a snippet or list watchers
bbx snippet watch abc123

# Manage snippet comments
bbx snippet comments abc123
```

### Workspaces

```bash
# List your workspaces
bbx workspace list

# View workspace details
bbx workspace view myworkspace

# List workspace members
bbx workspace members -w myworkspace

# Manage projects (list, view, create, delete)
bbx workspace projects -w myworkspace

# View workspace permissions
bbx workspace permissions -w myworkspace

# Manage webhooks
bbx workspace hooks -w myworkspace
```

## LLM Integration

All commands output JSON to stdout, errors to stderr. This makes them ideal for LLM tool use:

```bash
# Pipe to jq for pretty-printing
bbx pr view 123 -w myworkspace -r myrepo | jq .

# List commits for code review
bbx commit list -w myworkspace -r myrepo --branch feature-x --limit 5
```

## Global Options

The `-w`/`--workspace` and `-r`/`--repo` options are available on command groups that
need them. In `repo`, `pr`, `branch`, `commit`, and `issue`, they are implemented as
global options and appear directly in subcommand help.

| Option | Short | Description | Commands |
|--------|-------|-------------|----------|
| `--workspace` | `-w` | Bitbucket workspace (uses default if set) | repo, pr, branch, commit, issue |
| `--repo` | `-r` | Repository slug | pr, branch, commit, issue |

Other common options on subcommands:

| Option | Description |
|--------|-------------|
| `--limit` | Maximum results to return (default: 25) |
| `--state` | Filter by state (e.g., OPEN, MERGED, DECLINED) |
| `--yes` | Skip confirmation prompts on destructive operations |

## Configuration

Credentials are stored in `~/.config/bbx/config.json` with restricted permissions (600 on Unix).

## Building from Source

```bash
git clone https://github.com/solrevdev/solrevdev.bbx.git
cd solrevdev.bbx
dotnet build src/Bbx/Bbx.csproj

# Run directly without installing
dotnet run --project src/Bbx/Bbx.csproj -- pr list -w myworkspace -r myrepo

# Target a specific .NET version
dotnet run --project src/Bbx/Bbx.csproj -f net10.0 -- auth status

# Pack and install locally
dotnet pack src/Bbx/Bbx.csproj -c Release
dotnet tool install -g --add-source ./src/Bbx/bin/Release solrevdev.bbx
```

## License

MIT License - see [LICENSE](LICENSE) for details.
