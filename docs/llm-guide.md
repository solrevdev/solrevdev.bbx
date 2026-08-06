# `solrevdev.bbx` — LLM / agent integration guide

This document is the canonical reference for **LLM agents driving
`bbx` programmatically**. It is pattern-based, structured, and
example-heavy on purpose — the human-friendly walkthrough lives in
[`README.md`](../README.md).

Everything below matches the actual `bbx --help` output. If you spot
drift, regenerate by running `bbx <group> --help` and update the
relevant table.

---

## 1. Identity and contract

- `bbx` is a .NET global tool wrapping **Bitbucket Cloud REST v2**
  (`https://api.bitbucket.org/2.0/`).
- **stdout: JSON.** All read commands return JSON objects/arrays.
  Write commands return JSON too (the created/updated resource, or a
  `{deleted: true, …}` envelope). A tiny set return raw bytes
  (`bbx src cat`, `bbx pr patch`, `bbx commit patch`,
  `bbx download get` with no `--output`).
- **stderr: human messages.** All errors, prompts, progress lines,
  and the auth flow's status messages.
- **exit codes:** `0` on success, `1` on any handled error
  (`BbxUserException` for user-facing errors like "not authenticated",
  `HttpRequestException` for network/API failures wrapped with
  `"Error: "`).
- **No interactive prompts by default for an agent.** Set
  `BBX_NO_INTERACTIVE=1` or redirect stdin to disable the first-run
  credential prompt and the `--yes`-less confirmation prompts.

---

## 2. Authentication contract

`bbx` authenticates with Atlassian API tokens and nothing else.

| Method | Wire shape | Setup |
|---|---|---|
| Atlassian API token | `Authorization: Basic <base64(email:token)>` | `bbx auth login` (prompts for email + token) |

Scopes are chosen when the token is created. A call that needs a scope the
token lacks returns HTTP 403 naming the missing scope, so an agent can report
it rather than retrying.

Credentials live in `~/.config/bbx/config.json` (mode 600). The same
config file is read at every command invocation; nothing is held in
memory between runs.

### Recipe — non-interactive bootstrap for CI

```bash
# 1. Authenticate (one-time, on a build agent with no browser)
printf 'bot@example.com\nATATT3xFfGF0xxxx\n' | bbx auth login

# 2. Pin the default workspace so -w can be omitted
bbx auth set-workspace myworkspace

# 3. Verify
bbx auth status              # plain-text output; exit code is the contract
```

### Recipe — opt out of prompting

Set `BBX_NO_INTERACTIVE=1`. Any command that needs credentials but finds
none prints `Error: Not authenticated. …` and exits `1` instead of
prompting for a token.

---

## 3. Command surface (pattern-based)

Every command follows the same shape:

```
bbx <group> [<subgroup> ...] <verb> [<positional>] [<options>]
```

Each verb's options are stable and predictable; the patterns below let
an agent infer correct command lines without re-checking `--help`.

### 3.1 Workspace / repo selection

These three options appear (as global options) on `repo`, `pr`,
`branch`, `commit`, and `issue`:

| Option | Short | Required if no default set |
|---|---|---|
| `--workspace` | `-w` | Yes (or set with `bbx auth set-workspace`) |
| `--repo` | `-r` | Yes for `pr`/`branch`/`commit`/`issue` |
| `--json-compact` | — | Optional; toggles single-line JSON; env: `BBX_JSON_COMPACT=1` |

### 3.2 Common verb-name patterns

- `list` — paginated GET. Always supports `--limit <n>` (default 25).
- `view <id>` — single-resource GET.
- `create` — POST. Required body fields are `--<field>` options.
- `update <id>` — PUT/PATCH. Each field is a separate `--<field>`
  option; omit to leave a field unchanged.
- `delete <id>` — DELETE. Requires `--yes` to skip the confirmation
  prompt; without it, the command prompts on stderr and exits without
  acting if the user types anything other than `y` / `yes`.

### 3.3 Output shape patterns

List handlers return:
```json
{ "workspace": "...", "repository": "...", "count": <n>, "<entity>s": [ ... ] }
```

View handlers return a single object with the resource's fields.

Delete handlers return either `{"deleted": true, …}` (JSON) or a
plain-text confirmation line.

Where bbx **shapes** the Bitbucket response (most cases), property
names are snake_case. A handful of pass-through endpoints (snippets,
some pipelines payloads) keep the upstream shape verbatim.

---

## 4. Group reference

Every group below corresponds to one `Commands/*.cs` file. Each row
gives the verb, the Bitbucket endpoint it hits, and any important
flags. Workspace / repo options are omitted from the table — assume
they're available on `repo` / `pr` / `branch` / `commit` / `issue`.

### 4.1 `bbx auth`

| Verb | Endpoint | Flags / args |
|---|---|---|
| `login` | (validation via `user`) | `--email`, `--token`; prompts for whatever is omitted |
| `status` | `user` | plain-text output, not JSON |
| `token` | (local) | prints `email:token` for `curl -u` |
| `logout` | (local) | clears all stored credentials |
| `set-workspace <slug>` | (local) | sets `DefaultWorkspace` in config |

### 4.2 `bbx repo`

| Verb | Endpoint |
|---|---|
| `list` | `GET repositories/{ws}` |
| `view <slug>` | `GET repositories/{ws}/{repo}` |
| `create <name>` | `POST repositories/{ws}/{repo}` — `--private`, `--description`, `--scm`, `--language`, `--main-branch`, `--fork-policy`, `--project-key` |
| `delete <slug>` | `DELETE repositories/{ws}/{repo}` — `--yes` |
| `fork <slug>` | `POST repositories/{ws}/{repo}/forks` — `--name`, `--workspace-target` |
| `clone <slug>` | `GET repositories/{ws}/{repo}` (extracts clone URL) — `--protocol https|ssh` |
| `permissions <slug>` | `GET repositories/{ws}/{repo}/permissions-config/users` |
| `hooks {list,view,create,update,delete}` | `repositories/{ws}/{repo}/hooks[/{uid}]` |
| `default-reviewers {list,add,remove,effective}` | `repositories/{ws}/{repo}/(effective-)?default-reviewers[/{user}]` |
| `forks list` | `GET repositories/{ws}/{repo}/forks` |
| `watchers` | `GET repositories/{ws}/{repo}/watchers` |
| `branching-model {view,settings,update}` | `repositories/{ws}/{repo}/branching-model[/settings]` (PUT goes to `/settings`) |
| `deploy-keys {list,view,add,delete}` | `repositories/{ws}/{repo}/deploy-keys[/{id}]` |
| `update <slug>` | `PUT repositories/{ws}/{repo}` — `--name`, `--description`, `--private`/`--public`, `--fork-policy`, `--language`, `--website`, `--project`, `--main-branch`, `--issues`/`--no-issues`, `--wiki`/`--no-wiki` |
| `file-conflicts <spec>` | `GET repositories/{ws}/{repo}/file-conflicts/{spec}` — spec is `source..destination` |
| `override-settings {view,update}` | `GET/PUT repositories/{ws}/{repo}/override-settings` — `--default-reviewers`, `--branching-model`, `--branch-restrictions`, each `true` or `false` |
| `branching-model effective` | `GET repositories/{ws}/{repo}/effective-branching-model` |
| `default-reviewers view` | `GET repositories/{ws}/{repo}/default-reviewers/{user}` — `--target` |
| `access groups {list,view,set,remove}` | `repositories/{ws}/{repo}/permissions-config/groups[/{slug}]` — `--permission read\|write\|admin` |
| `access users {view,set,remove}` | `repositories/{ws}/{repo}/permissions-config/users/{account-id}` — `--permission read\|write\|admin` |

`repo update` merges: anything you leave out keeps its value. There is no
`repo deploy-keys update`: Bitbucket refuses to change a key's contents and
refuses a body without them, so delete and re-add instead. `repo
file-conflicts` answers 403 to an API token. `--permission none` is not
offered; use `remove`.

### 4.3 `bbx pr`

| Verb | Endpoint |
|---|---|
| `list` | `GET repositories/{ws}/{repo}/pullrequests` — `--state OPEN\|MERGED\|DECLINED\|SUPERSEDED`, `--author <account-id>` |
| `view <id>` | `GET repositories/{ws}/{repo}/pullrequests/{id}` |
| `create` | `POST repositories/{ws}/{repo}/pullrequests` — `--title`, `--source`, `--dest`, `--body`, `--close-source-branch`, `--reviewer <account-id>` (repeatable) |
| `update <id>` | `PUT repositories/{ws}/{repo}/pullrequests/{id}` — `--title`, `--body`, `--dest`, `--reviewers <account-id>...`, `--close-source-branch`, `--no-close-source-branch`. Open pull requests only. |
| `merge <id>` | `POST .../merge` — `--strategy merge_commit\|squash\|fast_forward`, `--message`, `--close-source-branch` |
| `approve <id>` / `unapprove <id>` | `POST/DELETE .../approve` |
| `decline <id>` | `POST .../decline` |
| `comments <id>` / `comment <id>` | `GET/POST .../comments` — `--body` for `comment` |
| `diff <id>` / `patch <id>` | `GET .../diff` or `.../patch` (raw text out) |
| `activity <id>` | `GET .../activity` |
| `statuses <id>` | `GET .../statuses` |
| `default-reviewers` | `GET repositories/{ws}/{repo}/effective-default-reviewers` (note: repo-scoped, no `<id>`) |
| `tasks {list,add,update,complete,delete} <id>` | `.../tasks[/{task-id}]` |
| `request-changes <id>` / `unrequest-changes <id>` | `POST/DELETE .../request-changes` |
| `commits <id>` | `GET .../commits` |
| `diffstat <id>` | `GET .../pullrequests/{id}/diffstat` — per-file line counts, cheaper than the whole diff |
| `conflicts <id>` | `GET .../pullrequests/{id}/conflicts` — answers 403 to an API token |
| `merge-status <id>` | `GET .../pullrequests/{id}/merge/task-status/{task-id}` — `--task-id` |
| `activity [<id>]` | `GET .../pullrequests/activity` with no ID, `.../pullrequests/{id}/activity` with one — `--limit` |
| `comment-view <id>` | `GET .../comments/{comment-id}` — `--comment-id` |
| `comment-update <id>` | `PUT .../comments/{comment-id}` — `--comment-id`, `--body` |
| `comment-delete <id>` | `DELETE .../comments/{comment-id}` — `--comment-id`, `--yes` |
| `comment-resolve <id>` / `comment-unresolve <id>` | `POST/DELETE .../comments/{comment-id}/resolve` — `--comment-id` |
| `tasks view <id>` | `GET .../tasks/{task-id}` — `--task-id` |

`pr update` sends only the flags you pass. Anything you leave out keeps its
current value, so you can retitle a pull request without touching its
description. An empty `--body ""` clears the description.

Two things to know before scripting it:

- `--close-source-branch` and `--no-close-source-branch` only take effect when
  the same call also changes a field to a new value. Bitbucket drops the setting
  otherwise and still answers 200, so `bbx` reads the pull request first and
  exits 1 rather than report a change that did not happen. Pair the flag with a
  real `--title`, `--body` or `--dest` change.
- `--reviewers` replaces the whole list and needs at least one account ID. There
  is no way to clear the reviewers, because an omitted `--reviewers` has to mean
  "leave them alone".

```bash
bbx pr update 42 -r myrepo --title "New title"
bbx pr update 42 -r myrepo --body ""                        # clear the description
bbx pr update 42 -r myrepo --dest release/next              # retarget
bbx pr update 42 -r myrepo --title "New title" --close-source-branch
```

### 4.4 `bbx branch`

| Verb | Endpoint |
|---|---|
| `list` | `GET repositories/{ws}/{repo}/refs/branches` — `--sort -name` etc. |
| `view <name>` | `GET .../refs/branches/{name}` |
| `create <name>` | `POST .../refs/branches` — `--target` |
| `delete <name>` | `DELETE .../refs/branches/{name}` — `--yes` |
| `restrictions {list,view,add,update,delete}` | `.../branch-restrictions[/{id}]` — `--kind`, `--pattern`, `--value`, `--users`, `--groups` |
| `refs` | `GET .../refs` — branches and tags in one list, `--query`, `--limit` |
| `tag {list,view,create,delete} <name>` | `.../refs/tags[/{name}]` |

### 4.5 `bbx commit`

| Verb | Endpoint |
|---|---|
| `list` | `GET repositories/{ws}/{repo}/commits[/{branch}]` — `--include`/`--exclude` walk a range and switch the call to `POST`, which is the only verb Bitbucket takes them on |
| `view <hash>` | `GET .../commit/{hash}` |
| `diff <hash>` / `patch <hash>` | `GET .../diff/{hash}` or `.../patch/{hash}` (raw) |
| `comments <hash>` | `GET .../commit/{hash}/comments` |
| `statuses <hash>` | `GET .../commit/{hash}/statuses` |
| `status {create,update} <hash>` | `POST/PUT .../commit/{hash}/statuses/build[/{key}]` — `--key`, `--state INPROGRESS\|SUCCESSFUL\|FAILED\|STOPPED`, `--url`, `--name`, `--description` |
| `filehistory <hash> <path>` | `GET .../filehistory/{hash}/{path}` |
| `merge-base <spec>` | `GET .../merge-base/{spec}` |
| `approve <hash>` / `unapprove <hash>` | `POST/DELETE .../commit/{hash}/approve` |
| `diffstat <spec>` | `GET .../diffstat/{spec}` |
| `pullrequests <hash>` | `GET .../commit/{hash}/pullrequests` |
| `comment <hash>` | `POST .../commit/{hash}/comments` — `--body`, `--path`, `--line` (a line needs a path) |
| `comment-view <hash>` | `GET .../commit/{hash}/comments/{comment-id}` — `--comment-id` |
| `comment-update <hash>` | `PUT .../commit/{hash}/comments/{comment-id}` — `--comment-id`, `--body` |
| `comment-delete <hash>` | `DELETE .../commit/{hash}/comments/{comment-id}` — `--comment-id`, `--yes` |
| `status view <hash>` | `GET .../commit/{hash}/statuses/build/{key}` — `--key` |

### 4.6 `bbx src`

| Verb | Endpoint |
|---|---|
| `ls [<path>]` | `GET .../src/{ref}/{path}`, or `GET .../src` with no `--ref`, which lists the root of the main branch without your having to know its name. A path still needs a ref. |
| `cat <path>` | `GET .../src/{ref}/{path}` (raw bytes) — `--ref` (required) |
| `write` | `POST .../src` (multipart) — `--branch`, `--message`, `--file <local>=<repo-path>` (repeatable), `--author` |

### 4.7 `bbx download`

| Verb | Endpoint |
|---|---|
| `list` | `GET .../downloads` |
| `upload` | `POST .../downloads` (multipart) — `--file <local-path>` |
| `get <filename>` | `GET .../downloads/{filename}` — `--output <path>` (else streams to stdout) |
| `delete <filename>` | `DELETE .../downloads/{filename}` — `--yes` |

### 4.8 `bbx issue` (DEPRECATED — Bitbucket issues API shuts down 2026-08-20)

`list / view / create / update / delete / comments / comment` —
endpoints under `repositories/{ws}/{repo}/issues[/{id}][/comments]`.
Each command prints a deprecation warning on stderr.

### 4.9 `bbx pipeline`

| Verb | Endpoint |
|---|---|
| `list` | `GET .../pipelines/` |
| `view <pipeline-uuid>` | `GET .../pipelines/{uuid}` |
| `trigger` | `POST .../pipelines/` — `--branch` (default `main`), `--commit`, `--pattern`, `--pull-request <id>`, `--variable KEY=value` (repeatable) |
| `stop <pipeline-uuid>` | `POST .../pipelines/{uuid}/stopPipeline` — `--yes` |
| `logs <pipeline-uuid> <step-uuid>` | `GET .../pipelines/{uuid}/steps/{step-uuid}/log`, or `.../logs/{log-uuid}` with `--log-uuid` for one numbered attempt |
| `steps <pipeline-uuid>` | `GET .../pipelines/{uuid}/steps/` |
| `step <pipeline-uuid> <step-uuid>` | `GET .../pipelines/{uuid}/steps/{step-uuid}` |
| `config {view,update,build-number}` | `GET/PUT .../pipelines_config`, `PUT .../pipelines_config/build_number` — `--enabled`/`--disabled`, `--next`. 404s until Pipelines has been enabled on the repository once. |
| `variables {list,view,add,update,delete}` | `.../pipelines_config/variables[/{uuid}]` — `--key`, `--value`, `--secured`/`--unsecured` |
| `schedules {list,view,create,update,delete,executions}` | `.../pipelines_config/schedules[/{uuid}[/executions]]` — `--cron`, `--branch`, `--pattern`, `--enabled`/`--disabled` |
| `ssh key-pair {view,set,delete}` | `.../pipelines_config/ssh/key_pair` — `--private-key`, `--public-key`. The private half is write-only. |
| `ssh known-hosts {list,view,add,update,delete}` | `.../pipelines_config/ssh/known_hosts[/{uuid}]` — `--hostname`, `--key-type`, `--key`. `bitbucket.org` is rejected: Bitbucket configures it already. |
| `caches {list,clear,content-uri}` | `.../pipelines-config/caches[/{uuid}[/content-uri]]`. `clear <uuid>` clears one; `clear --name node` clears every cache with that name through `DELETE .../caches?name=`. |
| `deployments {list,view,create,delete,changes}` | `.../environments[/{env}[/changes/]]` — `--name`, `--type Test\|Staging\|Production`, `--rank`. `changes` takes `--name`, `--admin-only`, `--no-admin-only` and nothing else: the lock, rank, type and hidden flag cannot be changed through it. |
| `deployments variables {list,add,update,delete}` | `.../deployments_config/environments/{env}/variables[/{uuid}]` — `--environment`/`-e`. The list endpoint always answers empty, whatever variables exist. |
| `deploys {list,view}` | `.../deployments[/{uuid}]` — the records of what was released where, as opposed to the environments |
| `reports {list,view,update,delete} <hash>` | `.../commit/{hash}/reports[/{report-id}]` — `update` takes a report ID of your choosing, so it creates as well as updates. `--title` and `--details` are both required. |
| `reports annotations <hash> <report-id>` | `GET .../reports/{id}/annotations` |
| `reports annotations-create <hash> <report-id>` | `POST .../reports/{id}/annotations` — `--annotations` takes a bare JSON array |
| `reports annotation-{view,update,delete} <hash> <report-id> <ann-id>` | `.../reports/{id}/annotations/{ann-id}` — `--summary`, `--details`, `--type`, `--severity`, `--path`, `--line` |
| `test-case-reasons <pipeline> <step> <test-case>` | `.../test_reports/test_cases/{uuid}/test_case_reasons` |
| `test-reports <pipeline-uuid> <step-uuid>` | `.../pipelines/{uuid}/steps/{uuid}/test_reports` |
| `test-cases <pipeline-uuid> <step-uuid>` | `.../pipelines/{uuid}/steps/{uuid}/test_reports/test_cases` |
| `oidc {config,keys}` | `.../pipelines-config/identity/oidc/.well-known/openid-configuration` / `.../keys.json` — answers 404: only the workspace form of this path exists, use `workspace pipelines oidc` |

### 4.10 `bbx snippet`

`list / view / create / update / delete / files / watch / comments` —
endpoints under `snippets/{workspace}[/{id}]`.

- `files <snippet-id> [<file-name>] [--raw] [--revision <rev>]` — list all
  files or stream a single file's contents.
- `watch <snippet-id> [--list|--unwatch]` — watch by default.
- `comments <snippet-id> [--add <body>|--delete <id>|--update <id> --content <body>]`.
- `commits <snippet-id> [--revision <rev>]` — the log, or one commit.
- `diff <snippet-id> <revision>` / `patch <snippet-id> <revision>` — raw text.
- `view`, `update` and `delete` take `--revision`, which pins the call to that
  node. Bitbucket uses it for optimistic concurrency: a write against a stale
  revision is refused rather than clobbering the newer one.

### 4.11 `bbx workspace`

| Verb | Endpoint |
|---|---|
| `list` | `GET workspaces` |
| `view <slug>` | `GET workspaces/{slug}` |
| `members` | `GET workspaces/{ws}/members` |
| `permissions` | `GET workspaces/{ws}/permissions` |
| `hooks {list,view,create,update,delete}` | `workspaces/{ws}/hooks[/{uid}]` |
| `project list` (alias: `projects list`) | `GET workspaces/{ws}/projects` |
| `project view <key>` | `GET workspaces/{ws}/projects/{key}` |
| `project create` | `POST workspaces/{ws}/projects` — `--key`, `--name`, `--description`, `--private` |
| `project delete <key>` | `DELETE workspaces/{ws}/projects/{key}` — `--yes` |
| `project default-reviewers {list,add,remove}` | `.../projects/{key}/default-reviewers[/{user}]` |
| `project branching-model {view,update}` | `.../projects/{key}/branching-model[/settings]` |
| `project deploy-keys {list,view,add,delete}` | `.../projects/{key}/deploy-keys[/{id}]` |
| `member <account-uuid>` | `GET workspaces/{ws}/members/{member}` — an account UUID or ID; usernames no longer work |
| `repo-permissions` | `GET workspaces/{ws}/permissions/repositories[/{repo}]` — `--repo` narrows it to one |
| `gpg-key` | `GET workspaces/{ws}/settings/gpg/public-key` — the key Bitbucket signs web-edit commits with |
| `pullrequests <account-uuid>` | `GET workspaces/{ws}/pullrequests/{user}` — every repository in the workspace, not just one. `--state` |
| `pipelines variables {list,view,add,update,delete}` | `workspaces/{ws}/pipelines-config/variables[/{uuid}]` — inherited by every repository |
| `pipelines oidc {config,keys}` | `workspaces/{ws}/pipelines-config/identity/oidc/...` — answers 403 to an API token |
| `project update <key>` | `PUT workspaces/{ws}/projects/{key}` — `--name`, `--description`, `--private`/`--public`, `--new-key` |
| `project branching-model settings` | `GET .../projects/{key}/branching-model/settings` |
| `project default-reviewers view` | `GET .../projects/{key}/default-reviewers/{user}` — `--target` |
| `project access groups {list,view,set,remove}` | `.../projects/{key}/permissions-config/groups[/{slug}]` — `--permission read\|write\|create-repo\|admin` |
| `project access users {list,view,set,remove}` | `.../projects/{key}/permissions-config/users[/{account-id}]` — same permissions |

### 4.12 `bbx user`

| Verb | Endpoint |
|---|---|
| `emails` | `GET user/emails` |
| `permissions workspaces` | `GET user/permissions/workspaces` |
| `permissions repositories` | `GET user/permissions/repositories` |
| `view <selected-user>` | `GET users/{selected_user}` |
| `ssh-keys {list,view,add,delete}` | `users/{me}/ssh-keys[/{uuid}]` (defaults to the current user; use `--user` to target someone else) |
| `emails --email <address>` | `GET user/emails/{email}` — is one address confirmed and primary |
| `workspaces` | `GET user/workspaces` — the account-scoped list, and the working replacement for the withdrawn `workspace list` |
| `permissions workspace` | `GET user/workspaces/{ws}/permission` — your role in one workspace |
| `permissions workspace-repositories` | `GET user/workspaces/{ws}/permissions/repositories` — your repository grants within one workspace |
| `gpg-keys {list,view}` | `users/{me}/gpg-keys[/{fingerprint}]` — read-only on purpose; the writes would mutate a real account |

---

## 5. Composition recipes

The patterns below are end-to-end flows agents commonly need. Each
uses `--json-compact` and `jq` to make the example easy to imitate;
drop `--json-compact` if you want pretty output.

### 5.1 "Find an open PR and approve it"

```bash
PR_ID=$(bbx pr list -w myworkspace -r myrepo --state OPEN --limit 1 --json-compact \
        | jq -r '.pull_requests[0].id')
bbx pr approve "$PR_ID" -w myworkspace -r myrepo
```

### 5.2 "List repos touched recently and dump their open PR titles"

```bash
bbx repo list -w myworkspace --limit 10 --json-compact \
  | jq -r '.repositories[].slug' \
  | while read -r repo; do
      bbx pr list -w myworkspace -r "$repo" --state OPEN --limit 5 --json-compact \
        | jq --arg repo "$repo" -r '.pull_requests[] | "\($repo): #\(.id) \(.title)"'
    done
```

### 5.3 "Trigger a pipeline on a PR's source branch and watch its status"

```bash
PR_ID=42
SOURCE=$(bbx pr view "$PR_ID" -w myworkspace -r myrepo --json-compact \
         | jq -r '.source.branch')
RESULT=$(bbx pipeline trigger -w myworkspace -r myrepo \
            --pull-request "$PR_ID" --branch "$SOURCE" --json-compact)
PIPELINE_UUID=$(jq -r '.pipeline.uuid' <<< "$RESULT")
bbx pipeline view "$PIPELINE_UUID" -w myworkspace -r myrepo
```

### 5.4 "Set a build status on the head commit of a branch"

```bash
HASH=$(bbx branch view main -w myworkspace -r myrepo --json-compact \
       | jq -r '.target.hash')
bbx commit status create "$HASH" -w myworkspace -r myrepo \
    --key my-ci --state SUCCESSFUL \
    --url https://ci.example/run/123 --name "Build #123"
```

### 5.5 "Add a webhook only if it doesn't exist"

```bash
URL=https://example.com/hook
EXISTS=$(bbx repo hooks list -w myworkspace -r myrepo --json-compact \
         | jq --arg url "$URL" '.hooks | any(.url == $url)')
if [ "$EXISTS" != "true" ]; then
  bbx repo hooks create -w myworkspace -r myrepo \
      --url "$URL" --events repo:push --events pullrequest:created
fi
```

### 5.6 "Read a file at a specific commit"

```bash
bbx src cat -w myworkspace -r myrepo --ref abc123 path/to/file.ts
# (raw bytes to stdout — no trailing newline added)
```

### 5.7 "Commit two files in one atomic push"

```bash
bbx src write -w myworkspace -r myrepo \
    --branch hotfix-x --message "patch config + readme" \
    --file ./config.local.json=config/prod.json \
    --file ./README.local.md=README.md \
    --author "Bot <bot@example.com>"
```

---

## 6. Errors and retries

- **`Error: Not authenticated. Run: bbx auth login`** — config is missing
  or empty. Fix: run `bbx auth login` once, or write the config file
  directly in CI.
- **`Error: … Missing token scopes: <scope>.`** — the token is valid but
  lacks a scope. Do not retry; the token has to be re-issued.
- **`Error: Workspace required. …`** — no `-w` and no default. Fix
  permanently with `bbx auth set-workspace <slug>`.
- **`Error: Workspace and repository required.`** — `pr`/`branch`/
  `commit`/`issue` need `-r` (and `-w` if not defaulted).
- **`Error: <Bitbucket API error message>`** — a 4xx/5xx from
  Bitbucket. The body is bubbled through verbatim after `"Error: "`.
- **`Error: … This resource does not support authentication using the
  provided token`** — an HTTP 403 from an endpoint that refuses API tokens
  outright. `pr conflicts`, `repo file-conflicts` and the OIDC discovery
  commands are the ones we found. Do not retry; it needs a different
  credential type.
- HTTP 429 / 5xx are NOT auto-retried in `bbx`. An agent should
  backoff and retry from its own loop.

Error text carries the status, and now also `error.detail` and any
`error.data.arguments` Bitbucket sent. Several endpoints answer a bare "Bad
request" with the real reason only in an argument, so read the whole line
before deciding a call is unfixable.

A revoked or expired token surfaces as an HTTP 401 on the next call. The
fix is to re-run `bbx auth login`.

---

## 7. Versioning / stability

- The **JSON shape** of read commands is stable within a major
  version. Adding fields is non-breaking; removing or renaming is
  breaking and gets a major bump.
- The **command surface** (group/verb/flag names) follows the same
  rule. Aliases like `workspace projects` (alias of `workspace
  project`) MAY be removed in a future major.
- Within v2.x, `bbx issue *` is the only command group that will
  disappear (alongside Bitbucket's Issues API on 2026-08-20). The
  deprecation warning gives advance notice.

For the full list of changes by version, see
[`CHANGELOG.md`](../CHANGELOG.md).
