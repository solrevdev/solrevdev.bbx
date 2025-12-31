# 🔧 solrevdev.bbx

[![NuGet Version](https://img.shields.io/nuget/v/solrevdev.bbx)](https://www.nuget.org/packages/solrevdev.bbx)
[![NuGet Downloads](https://img.shields.io/nuget/dt/solrevdev.bbx)](https://www.nuget.org/packages/solrevdev.bbx)

A .NET global tool providing Bitbucket Cloud API v2 access, designed for LLM integration. All output is JSON for easy parsing by AI assistants like Claude, GitHub Copilot, or custom workflows.

## 📦 Installation

```bash
dotnet tool install -g solrevdev.bbx
```

## 🔑 Authentication

Before using `bbx`, authenticate with your Bitbucket account using an [App Password](https://support.atlassian.com/bitbucket-cloud/docs/app-passwords/):

```bash
# Login with app password (recommended)
bbx auth login --app-password

# Set default workspace
bbx auth set-workspace myworkspace

# Check authentication status
bbx auth status

# View stored token (for debugging)
bbx auth token
```

### Required App Password Permissions

Create an app password at Bitbucket Settings → App passwords with these scopes:

| Scope | Permission | For |
|-------|------------|-----|
| Account | Read | User info |
| Repositories | Read, Write, Admin | Repo management |
| Pull Requests | Read, Write | PR operations |
| Issues | Read, Write | Issue tracking |
| Pipelines | Read, Write | CI/CD |
| Snippets | Read, Write | Code snippets |

## 📋 Commands

### Repository Management

```bash
# List repositories in workspace
bbx repo list --workspace myworkspace

# View repository details
bbx repo view myworkspace/myrepo

# Create a new repository
bbx repo create myrepo --workspace myworkspace --private

# Get clone URLs
bbx repo clone-url myrepo --workspace myworkspace

# Fork a repository
bbx repo fork owner/repo --workspace myworkspace

# Delete repository (with confirmation)
bbx repo delete myrepo --workspace myworkspace
```

### Pull Requests

```bash
# List open PRs
bbx pr list --workspace myworkspace --repo myrepo

# View PR details
bbx pr view 123 --workspace myworkspace --repo myrepo

# Create a new PR
bbx pr create --source feature-branch --dest main \
    --title "Add new feature" --description "Description here" \
    --workspace myworkspace --repo myrepo

# Approve/unapprove PR
bbx pr approve 123 --workspace myworkspace --repo myrepo
bbx pr unapprove 123 --workspace myworkspace --repo myrepo

# Merge PR
bbx pr merge 123 --strategy squash --workspace myworkspace --repo myrepo

# View PR diff
bbx pr diff 123 --workspace myworkspace --repo myrepo

# List PR comments
bbx pr comments 123 --workspace myworkspace --repo myrepo

# Add a comment
bbx pr comments 123 --add "LGTM!" --workspace myworkspace --repo myrepo
```

### Branch Management

```bash
# List branches
bbx branch list --workspace myworkspace --repo myrepo

# View branch details
bbx branch view main --workspace myworkspace --repo myrepo

# Create a branch
bbx branch create feature-x --from main --workspace myworkspace --repo myrepo

# Delete a branch
bbx branch delete feature-x --workspace myworkspace --repo myrepo

# List branch restrictions
bbx branch restrictions --workspace myworkspace --repo myrepo

# Add branch restriction
bbx branch restrictions --add push --pattern "main" \
    --workspace myworkspace --repo myrepo
```

### Commits

```bash
# List recent commits
bbx commit list --workspace myworkspace --repo myrepo --limit 20

# View commit details
bbx commit view abc123 --workspace myworkspace --repo myrepo

# Get commit diff
bbx commit diff abc123 --workspace myworkspace --repo myrepo

# List commits on a branch
bbx commit list --branch feature-x --workspace myworkspace --repo myrepo

# View commit build statuses
bbx commit statuses abc123 --workspace myworkspace --repo myrepo
```

### Issues

```bash
# List issues
bbx issue list --workspace myworkspace --repo myrepo

# Filter by state
bbx issue list --state open --workspace myworkspace --repo myrepo

# View issue details
bbx issue view 42 --workspace myworkspace --repo myrepo

# Create an issue
bbx issue create --title "Bug report" --content "Description..." \
    --priority major --kind bug --workspace myworkspace --repo myrepo

# Update issue state
bbx issue update 42 --state resolved --workspace myworkspace --repo myrepo

# Add comment
bbx issue comments 42 --add "Working on this" \
    --workspace myworkspace --repo myrepo
```

### Pipelines

```bash
# List recent pipelines
bbx pipeline list --workspace myworkspace --repo myrepo

# View pipeline details
bbx pipeline view {uuid} --workspace myworkspace --repo myrepo

# Trigger a pipeline
bbx pipeline trigger --branch main --workspace myworkspace --repo myrepo

# Get pipeline logs
bbx pipeline logs {uuid} --step 1 --workspace myworkspace --repo myrepo

# Stop a running pipeline
bbx pipeline stop {uuid} --workspace myworkspace --repo myrepo

# List deployments
bbx pipeline deployments --workspace myworkspace --repo myrepo
```

### Snippets

```bash
# List snippets
bbx snippet list --workspace myworkspace

# View snippet
bbx snippet view abc123 --workspace myworkspace

# Create snippet from files
bbx snippet create --title "My snippet" --file script.sh --file config.json \
    --private --workspace myworkspace

# List files in snippet
bbx snippet files abc123 --workspace myworkspace

# Get file content
bbx snippet files abc123 script.sh --raw --workspace myworkspace

# Delete snippet
bbx snippet delete abc123 --workspace myworkspace
```

### Workspaces

```bash
# List your workspaces
bbx workspace list

# View workspace details
bbx workspace view myworkspace

# List workspace members
bbx workspace members --workspace myworkspace

# List projects
bbx workspace projects --workspace myworkspace

# Create a project
bbx workspace projects --create "My Project" --key PROJ \
    --workspace myworkspace

# View workspace permissions
bbx workspace permissions --workspace myworkspace

# Manage webhooks
bbx workspace hooks --workspace myworkspace
bbx workspace hooks --create "https://example.com/webhook" \
    --events repo:push --workspace myworkspace
```

## 🎯 LLM Integration

All commands output JSON, making them ideal for LLM tool use:

```bash
# Example: Get PR for LLM analysis
bbx pr view 123 --workspace myworkspace --repo myrepo | jq .

# Example: List commits for code review
bbx commit list --branch feature-x --limit 5 --workspace myworkspace --repo myrepo
```

### Using with Claude Code

Add to your Claude Code configuration:

```json
{
  "tools": {
    "bbx": {
      "command": "bbx",
      "description": "Bitbucket Cloud CLI for repository management"
    }
  }
}
```

## ⚙️ Global Options

Most commands support these common options:

- `--workspace, -w` - Bitbucket workspace (uses default if not specified)
- `--repo, -r` - Repository slug
- `--limit, -l` - Maximum results to return
- `--yes, -y` - Skip confirmation prompts

## 📁 Configuration

Credentials are stored securely in `~/.config/bbx/config.json` with restricted permissions (600 on Unix).

```bash
# Logout and clear credentials
bbx auth logout
```

## 🔧 Building from Source

```bash
git clone https://github.com/solrevdev/solrevdev.bbx.git
cd solrevdev.bbx
dotnet build
dotnet pack

# Install locally
dotnet tool install -g --add-source ./src/Bbx/bin/Release solrevdev.bbx
```

## 📝 License

MIT License - see [LICENSE](LICENSE) for details.

## 🤝 Contributing

Contributions welcome! Please open an issue or PR on GitHub.
