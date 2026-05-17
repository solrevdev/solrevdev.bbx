# solrevdev.bbx

[![NuGet Version](https://img.shields.io/nuget/v/solrevdev.bbx)](https://www.nuget.org/packages/solrevdev.bbx)
[![NuGet Downloads](https://img.shields.io/nuget/dt/solrevdev.bbx)](https://www.nuget.org/packages/solrevdev.bbx)

A .NET global tool providing Bitbucket Cloud API v2 access, designed for LLM integration. All output is JSON for easy parsing by AI assistants like Claude, GitHub Copilot, or custom workflows.

## Installation

```bash
dotnet tool install -g solrevdev.bbx
```

## Authentication

`bbx` supports two authentication methods:

- **OAuth 2.0 (recommended)** — interactive browser-based login, refreshes
  automatically. Bitbucket retires app passwords on 2026-06-09, so OAuth
  (or an API token) is the path forward.
- **Atlassian API token** — fallback for CI / scripted setups where there
  is no browser.

### First-time setup with OAuth

OAuth requires a one-time consumer registration in your workspace. Run:

```bash
bbx auth setup-oauth          # prints the walkthrough
bbx auth setup-oauth --open   # also opens the workspace API settings page
```

The walkthrough tells you to create a private OAuth consumer with the
callback URL `http://localhost:53682/callback` (this is fixed — match it
exactly). Then run:

```bash
bbx auth login --oauth
# or pass credentials non-interactively:
bbx auth login --oauth --client-id <key> --client-secret <secret>
```

`bbx auth login --oauth` opens your browser, captures the authorization
code on the loopback listener, exchanges it for an access + refresh token,
and stores both in `~/.config/bbx/config.json` (mode 600).

You don't actually have to run `bbx auth login --oauth` explicitly the
first time — any `bbx` command (e.g. `bbx repo list -w myworkspace`)
auto-launches the OAuth flow if no credentials are stored, then continues
with the original command. Set `BBX_NO_INTERACTIVE=1` (or run with stdin
redirected) to opt out and get the existing not-authenticated error
instead — useful for CI.

Other OAuth subcommands:

```bash
bbx auth refresh    # force a refresh, print new expires_at
bbx auth token      # print current access token (refreshing first if needed)
```

### Fallback: API token

For CI / scripted environments without a browser, use an
[Atlassian API token](https://bitbucket.org/account/settings/api-tokens/):

```bash
bbx auth login --api-token
# Enter your Atlassian account email and the API token
```

Create the token at <https://bitbucket.org/account/settings/api-tokens/>
with the scopes you need:

| Scope | Permission | For |
|-------|------------|-----|
| Account | Read | User info, auth status |
| Repositories | Read, Write, Admin | Repo management |
| Pull Requests | Read, Write | PR operations |
| Issues | Read, Write | Issue tracking |
| Pipelines | Read, Write | CI/CD |
| Snippets | Read, Write | Code snippets |

### Common auth commands

```bash
bbx auth set-workspace myworkspace   # default workspace for -w
bbx auth status                       # show auth method, expiry (OAuth), user
bbx auth token                        # access token / username:api_token
bbx auth logout                       # clear credentials and consumer secret
```

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

# Manage webhooks on a repo
bbx repo hooks list -w myworkspace -r myrepo
bbx repo hooks create -w myworkspace -r myrepo --url https://example.com/hook --events repo:push --events pullrequest:created
bbx repo hooks view {uuid} -w myworkspace -r myrepo
bbx repo hooks update {uuid} -w myworkspace -r myrepo --active false
bbx repo hooks delete {uuid} -w myworkspace -r myrepo --yes

# Manage default reviewers
bbx repo default-reviewers list -w myworkspace -r myrepo
bbx repo default-reviewers add -w myworkspace -r myrepo --target {account-id-or-uuid}
bbx repo default-reviewers remove -w myworkspace -r myrepo --target {account-id-or-uuid} --yes
bbx repo default-reviewers effective -w myworkspace -r myrepo
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

# Show effective default reviewers (repo + project, inherited)
bbx pr default-reviewers -w myworkspace -r myrepo

# Manage PR tasks (review checklist items)
bbx pr tasks list 123 -w myworkspace -r myrepo
bbx pr tasks add 123 -w myworkspace -r myrepo --content "Please add a test"
bbx pr tasks update 123 -w myworkspace -r myrepo --task-id 7 --content "Updated wording"
bbx pr tasks complete 123 -w myworkspace -r myrepo --task-id 7
bbx pr tasks delete 123 -w myworkspace -r myrepo --task-id 7 --yes

# Request / unrequest changes
bbx pr request-changes 123 -w myworkspace -r myrepo
bbx pr unrequest-changes 123 -w myworkspace -r myrepo

# List PR commits, fetch raw patch
bbx pr commits 123 -w myworkspace -r myrepo
bbx pr patch 123 -w myworkspace -r myrepo
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

# Manage tags (refs/tags)
bbx branch tag list -w myworkspace -r myrepo
bbx branch tag view v1.0.0 -w myworkspace -r myrepo
bbx branch tag create v1.0.0 -w myworkspace -r myrepo --target main --message "Release 1.0.0"
bbx branch tag delete v1.0.0 -w myworkspace -r myrepo --yes
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

# Create / update a build status on a commit (CI integrations)
bbx commit status create abc123 -w myworkspace -r myrepo \
    --key my-ci --state INPROGRESS --url https://ci.example/run/42 \
    --name "Build #42" --description "Running unit tests"
bbx commit status update abc123 -w myworkspace -r myrepo \
    --key my-ci --state SUCCESSFUL

# List pull requests for a commit
bbx commit pullrequests abc123 -w myworkspace -r myrepo
```

### Source / files

```bash
# List a directory at a ref
bbx src ls -w myworkspace -r myrepo --ref main src/

# Print a file at a ref
bbx src cat -w myworkspace -r myrepo --ref main src/Program.cs

# Commit one or more files in a single multipart request
bbx src write -w myworkspace -r myrepo \
    --branch feature-x --message "tweak config" \
    --file ./local/path.json=config/path.json \
    --file ./README.md=README.md \
    --author "Jane Doe <jane@example.com>"
```

### Downloads

```bash
# List repo download artifacts
bbx download list -w myworkspace -r myrepo

# Upload (multipart)
bbx download upload -w myworkspace -r myrepo --file ./dist/release.tar.gz

# Get raw bytes to stdout, or save to a file
bbx download get release.tar.gz -w myworkspace -r myrepo --output ./release.tar.gz
bbx download get release.tar.gz -w myworkspace -r myrepo > release.tar.gz

# Delete
bbx download delete release.tar.gz -w myworkspace -r myrepo --yes
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
