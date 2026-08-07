# CLAUDE.md

Context for Claude Code working on `solrevdev.bbx`.

## What this is

A .NET global tool wrapping the Bitbucket Cloud API v2. Think `gh`, for
Bitbucket. Output is JSON so scripts and agents can consume it.

## Commands

```bash
dotnet build src/Bbx/Bbx.csproj                    # net10.0; SDK pinned by global.json
dotnet test tests/Bbx.Tests/Bbx.Tests.csproj       # xunit v3, no network
dotnet run --project src/Bbx/Bbx.csproj -f net10.0 -- <args>
dotnet pack src/Bbx/Bbx.csproj -c Release
```

## Layout

```
src/Bbx/
  Program.cs        entry point; builds DI once, owns the exit-code contract
  Api/              BitbucketClient, JsonElementExtensions
  Auth/             ICredentialStore, CredentialManager, IAuthProvider, AuthGate
  Commands/         System.CommandLine wiring ONLY, no business logic
    CommandBinding.cs  SetHandler/Bound<T> binding over System.CommandLine 2.0
  Features/<Area>/<Verb>/   Request record + Handler, co-located
  Composition/      ServiceRegistration (DI), JsonOptions
tests/Bbx.Tests/    FakeHttpMessageHandler, InMemoryCredentialStore, CaptureConsole
docs/spec/          pinned Bitbucket API spec; query it, do not fetch the docs site
```

Before guessing at an endpoint, read it: `jq '.paths["<path>"]' docs/spec/swagger.json`.
`scripts/fetch-spec.sh` refreshes the copy and records the date beside it. The
spec is often wrong about bodies and required fields, which is what most of the
rules below are; it is still the right place to start.

## Rules that bite

1. **Handlers hold the logic.** A file in `Commands/` parses arguments and calls
   a handler through `CommandRunner`. Nothing else.
2. **Endpoints carry no leading slash.** `NormalizeEndpoint` strips one, because
   a leading `/` breaks `HttpClient` BaseAddress resolution. Absolute URLs pass
   through unchanged (pagination `next` links).
3. **Use `TryGetObject`, not `TryGetProperty`, for nested objects.**
   `TryGetProperty` returns true for a JSON `null`, and reading through it
   throws. Bitbucket sends explicit nulls: a lightweight tag has
   `"tagger": null`.
4. **Non-JSON GETs send `Accept: */*`.** The default `application/json` makes the
   pipeline step log endpoint answer 406 instead of falling back.
5. **Redirects are followed in-process** (`AllowAutoRedirect = false`), because
   HttpClient drops `Authorization` when it follows one. Credentials are
   re-applied on the same origin only. This is what makes `pr diff` and
   `pr patch` work at all.
6. **`PostAsync`/`PutAsync` tolerate an empty body.** A 204 deserialised as `""`
   throws.
7. **Exit codes.** A value returned from `Main` overrides `Environment.ExitCode`,
   so `Main` keeps a non-zero invocation result and otherwise returns what the
   handler set. System.CommandLine 2.0 has no built-in exception handler, so
   `Main` wraps the invocation and prints the message rather than a stack trace.
   Ctrl+C is the library's job, not ours: `InvocationPipeline` links the token
   passed to `InvokeAsync` to a source of its own and cancels it from
   `ProcessTerminationHandler`, which registers for SIGINT and SIGTERM. Passing
   `default` still gives handlers a cancelable token. Do not add a
   `Console.CancelKeyPress` hook; it only competes with that registration.
   A cancelled run exits 130 and says nothing.
8. **Errors carry context.** `EnsureSuccessAsync` appends the HTTP status, names
   missing scopes from `error.detail.required`, and prints
   `error.data.announcement_url` for deprecations. `BitbucketErrorDetail.Detail`
   must stay a `JsonElement`, because it is an object for scope failures and a string
   everywhere else.
9. **Destructive verbs take `--yes`** and confirm otherwise.
10. **Tests do not run in parallel** (`AssemblyInfo.cs`), because several capture
    `Console` or touch `Environment.ExitCode`.
    Assertions come from **AwesomeAssertions**, an Apache-2.0 fork of
    FluentAssertions 7. Do not "upgrade" to FluentAssertions 8: it moved to the
    Xceed Community licence, which needs a paid commercial licence for use by or
    for a revenue-earning organisation. The API is the same; the namespace is
    `AwesomeAssertions`.
11. **Commands are bound through `CommandBinding`.** 2.0 replaced the typed
    `SetHandler(handler, symbols…)` family with a single `SetAction(ParseResult…)`
    callback. `Commands/CommandBinding.cs` keeps the declarative shape and the
    compile-time check that each bound symbol matches its handler parameter.
    A broken command definition still compiles, so parse-level behaviour is
    covered by `CommandSurfaceTests`.
12. **An absent array option parses to an empty array, not null.** A handler that
    keys off null therefore treats "flag not given" as "set this to nothing".
    `UpdatePullRequestHandler` only sends `reviewers` when the array is non-empty,
    because the null check shipped a bug that stripped the reviewers off every
    pull request it touched.
13. **`PUT pullrequests/{id}` merges, and drops `close_source_branch` on its own.**
    Fields left out of the body keep their value, so send only what changed.
    `close_source_branch` is the exception: Bitbucket applies it only when the
    same call also moves another field to a *new* value. On its own, or beside a
    field set to what it already holds, it is discarded, and the 200 response
    still echoes the value you sent. `UpdatePullRequestHandler` reads the pull
    request first and refuses rather than report a change that did not land.
    Only open pull requests can be updated at all.

14. **A write that answers 204 has no body**, so the client deserializes it to
    the default `JsonElement`. Serializing that throws "Operation is not valid
    due to the current state of the object", which turns a call that worked
    into an unexplained error. `CommandRunner` prints null instead;
    `override-settings update` reads the settings back, because the caller
    wanted to see them anyway.
15. **Bitbucket resolves several payloads by a `type` discriminator.** A report
    needs `"type": "report"`, an annotation `"report_annotation"`, a known host
    `"pipeline_known_host"` with `"pipeline_ssh_public_key"` nested inside. A
    body without one is answered with a 400 carrying no message at all.
16. **A report needs `details`.** The spec marks nothing required and the field
    reads as optional; Bitbucket answers "Cannot build Report, some of required
    attributes are not set [details]". `--details` is therefore required.
17. **`DELETE pipelines-config/caches` is not "clear everything".** It takes
    `?name=` and clears every cache with that name whatever its UUID, and
    answers a bare 400 without it.
18. **`/user/workspaces` returns `workspace_access` records, not workspaces.**
    The slug and uuid sit under a nested `workspace`; the top level carries only
    whether the caller is an administrator. This is also the working
    replacement for the withdrawn `/2.0/workspaces`.
19. **`PUT deploy-keys/{id}` cannot succeed, and the command is gone.** Without
    `key` Bitbucket says the key is invalid; with it, that you may not change a
    key's contents. Eight bodies were tried on 2026-08-06, including the key
    with its comment appended exactly as `ssh-keygen` wrote it, an empty key, a
    different key and a `type` discriminator. All 400. The web UI offers no
    rename control either, only view and delete. Do not add the verb back.
    Delete and re-add.
20. **Some endpoints refuse an API token**, answering 403 "This resource does
    not support authentication using the provided token". Pull request and file
    conflicts and the *workspace* OIDC discovery endpoints are the ones found so
    far. It is not a scope problem and no scope fixes it. Do not confuse this
    with a path that does not exist: the repository-scoped
    `repositories/{ws}/{repo}/pipelines-config/identity/oidc/...` answers 404
    "There is no API hosted at this URL", because only the workspace form is
    real. `bbx pipeline oidc` called it and has been removed; do not add it
    back. `bbx workspace pipelines oidc` is the one that reaches a real path.
21. **Errors carry `detail` and `data.arguments` as well as the message.**
    Bitbucket often answers a bare "Bad request" and puts the reason in an
    argument, which is how "SSH for this hostname is already configured by
    Bitbucket" stayed invisible until `EnsureSuccessAsync` printed it.
22. **`POST environments/{uuid}/changes/` takes a change envelope.** The body is
    `{"change": {...}}`, not a set of fields, and it answers 202 with an empty
    body because the change is queued. The spec documents no body at all; this
    was read off the web UI, which posts exactly that. Only `name` and
    `restrictions.admin_only` can be changed. `lock`, `rank`, `hidden`,
    `environment_type` and `environment_lock_enabled` are all answered with 400
    `deploy-service.environment.change-not-supported`, and there is no lock
    resource anywhere in 2.0. A body without `change` gets a different 400,
    `deploy-service.request.validation-error`, which is how you tell a wrong
    envelope from an unchangeable field. The trailing slash is optional.
23. **`none` is not a permission.** `permissions-config` takes `read`, `write`,
    `admin`, and `create-repo` on projects. `none` is answered with 400 "none is
    not a valid permission". Use `DELETE` to clear a grant.
24. **The permissions-config spec text about app passwords is stale.** All eight
    operations claim "The only authentication method for this endpoint is via
    app passwords". App passwords were withdrawn on 28 July 2026 and an API
    token drives them fine; all eight were run live on 2026-08-06. A user-keyed
    grant cannot name the workspace owner: Bitbucket answers 400 "This user is
    linked to this workspace, so their access ... cannot be modified or
    removed", so a second member is the only way to test those four. The
    selector may be an account UUID with its braces or an account ID; both
    work, and a username does not.
25. **Groups live only in the 1.0 API.** No 2.0 path mentions groups outside
    `permissions-config`, so there is no way to list a group slug from 2.0.
    `GET /1.0/groups/{workspace}/` still answers 200 with an API token and is
    the only way to read one. `GET /1.0/users/{workspace}/invitations` answers
    too, and lists invitations that have been sent but not accepted. Both are
    probes, not features: do not build on them.
26. **Deleting a pull request or commit comment leaves a tombstone; deleting a
    snippet comment does not.** `DELETE .../comments/{id}` answers 204 either
    way, but on pull requests and commits the row stays: a later `comments`
    still counts it and returns `"content": ""` with `"deleted": true`. On
    snippets the row goes, a later `--view` answers 404, and the count drops.
    Do not treat a non-zero count as proof the delete failed, and do not filter
    the empty rows out: the caller needs to see that something was there. The
    three list handlers pass `deleted` through, because empty content on its
    own does not say whether someone deleted the comment or left it blank.
    All three paths were run end to end on 2026-08-07 against a throwaway
    repository and snippet, both since deleted.
27. **`bbx` is remote-only, and that is a decision, not an oversight.** It never
    runs `git`, never reads a remote, and cannot tell which clone it was
    launched from. `--close-source-branch` is a field on Bitbucket's merge
    endpoint, executed on Bitbucket; it cannot touch a working copy.
    `gh` calls the nearest thing `-d, --delete-branch` and that one deletes the
    local branch too, so people arrive expecting it. The gap was considered on
    2026-08-07 and left alone: `gh` only deletes locally because it works the
    other way round, resolving the repository *from* the clone's remotes, and it
    switches local deletion off entirely when `--repo` is passed
    (`CanDeleteLocalBranch = !cmd.Flags().Changed("repo")`). Every `bbx` command
    takes `-r`, so under that rule nothing would ever be deleted anyway. Adding
    it would mean shelling out to `git`, parsing `origin`, and proving the clone
    is the repository that was just merged. The help text and README say so
    instead. If this is revisited, do not add a `-d` that only closes the remote
    branch: a flag matching `gh`'s name that does half the job invites the same
    mistake it was meant to fix.
28. **Do not default a branch option to "main".** `pipeline trigger` and
    `pipeline schedules create` both did, and new repositories in
    `foremost-group` are created with `mainbranch.name` of **master**, so the
    default was wrong on every one of them. Worse than a 404:
    `POST pipelines_config/schedules` accepts a `ref_name` for a branch that
    does not exist and answers 201, so the old default quietly created
    schedules firing on a cron against nothing. Verified live on 2026-08-07
    against a throwaway repository, since deleted.
    `Features/Common/DefaultBranch` asks the API instead: `mainbranch.name` off
    the repository, or `source.branch.name` off the pull request for a
    pull-request run, since a PR pipeline runs on the PR's own branch. `src ls`
    already relied on the API knowing its own default.
29. **There is no pull-request pipeline target.** `pipeline trigger
    --pull-request` sent `"type": "pipeline_pullrequest_target"` and had never
    worked in any released version: Bitbucket answers 400 "The request body
    contains invalid properties", and the type appears nowhere in
    `docs/spec/swagger.json`, which has `pipeline_ref_target` and
    `pipeline_commit_target` only. Four bodies were tried live on 2026-08-07,
    with the id as a string and as a number, with and without `source`. All 400.
    A pull-request run is a **`pipeline_ref_target` on the source branch with a
    `pull-requests` selector**, whose pattern matches the source branch in the
    `pull-requests:` section of `bitbucket-pipelines.yml` (`**` is the
    catch-all). Proven by running both on one commit: the selector build ran the
    `pull-requests:` step, a plain branch trigger ran the `default:` one. The PR
    id never reaches the wire; it is only how `bbx` looks the source branch up.
30. **Bitbucket does not check that a commit is on the branch you name.** A
    `pipeline_ref_target` with `ref_name: master` and a commit that exists only
    on a feature branch is answered 201 and runs, labelled `master`. So a
    guessed branch beside an explicit commit is silently wrong rather than
    rejected, which is why `--commit` requires `--branch`.
31. **Merge strategies live on the branch, not the pull request.**
    `GET refs/branches/{name}` returns `default_merge_strategy` and
    `merge_strategies`; a live branch listed six, not the three the help used to
    claim: `merge_commit`, `squash`, `fast_forward`, `squash_fast_forward`,
    `rebase_fast_forward`, `rebase_merge`. The pull request does **not** carry
    them whatever the spec says: `destination.branch` comes back as
    `{"name": "master"}` alone, checked on both `create` and `view`. A strategy
    the branch forbids is answered "merge_strategy: Select a valid choice",
    which does not name the choices, so `MergePullRequestHandler` reads the
    branch and refuses first. There is no 2.0 endpoint that *sets* the allowed
    strategies; that is web UI only.
32. **A schedule's branch cannot be changed.** `PUT
    pipelines_config/schedules/{uuid}` accepts a whole new `target`, answers
    200, echoes the **old** `ref_name` back and moves nothing; a read afterwards
    confirms it. Tried with the target alone and beside `type` and
    `cron_pattern`. Do not add a `--branch` to `schedules update`: delete the
    schedule and create another. `POST` is worse than lax about it and accepts a
    `ref_name` for a branch that does not exist, answering 201, so a wrong
    branch there fires on a cron against nothing.

## Auth

API tokens only. OAuth was removed: Bitbucket requires every user to create their
own OAuth consumer first, which is more work than pasting a token and needs
workspace admin rights a contributor may not have.

- Basic auth, `email:token`. Atlassian API tokens are **not** Bearer tokens.
- Stored at `~/.config/bbx/config.json`, mode 0600.
- `AuthGate` runs before every non-auth command: prompts on a TTY, errors without
  one (`BBX_NO_INTERACTIVE=1` forces the latter).
- `ConfigAuthProvider` picks `BasicAuthProvider` or `NullAuthProvider` from the
  config and caches it; `AuthGate` invalidates that cache after a login.

## Endpoints Bitbucket has withdrawn

Do not try to "fix" these. They return 410 Gone:

- `/2.0/workspaces` (`bbx workspace list`)
- `/2.0/user/permissions/{workspaces,repositories}`
- `/2.0/repositories?role=…` without a workspace
- `/2.0/snippets` (`bbx snippet list`), CHANGE-2770, seen 2026-08-07. Listing
  only. Every other snippet path still works, including create, view, comments
  and delete, so you need the snippet ID from somewhere else.

Usernames are no longer valid user selectors either; use an account UUID or
account ID. Bitbucket Issues shut down **2026-08-20** and the `issue` group goes
with them.

## Adding a command

1. `Features/<Area>/<Verb>/<Verb>Request.cs`, a record.
2. `Features/<Area>/<Verb>/<Verb>Handler.cs`: constructor-inject
   `BitbucketClient` and `CredentialManager`; resolve workspace/repo through
   `Resolve`; return an anonymous object.
3. Register it in `ServiceRegistration.RegisterHandlers`.
4. Wire it in `Commands/<Area>Command.cs` via `CommandRunner.Run*Async`.
5. Test it against `FakeHttpMessageHandler`.
