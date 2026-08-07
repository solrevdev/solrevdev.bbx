# Changelog

All notable changes to `solrevdev.bbx` are documented in this file.
The format loosely follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
this project uses [Conventional Commits](https://www.conventionalcommits.org/),
so commit history is the source of truth for the fine grain.

Versioning follows [Semantic Versioning](https://semver.org/): `MAJOR.MINOR.PATCH`.

## [1.3.3]

### Fixed

- `pipeline trigger --help` told you to do something the command refuses.
  `--branch` still claimed it overrode the pull request's source branch, and
  `--pull-request` still said `--branch` could override it, both written before
  the two flags were made mutually exclusive.

## [1.3.1]

### Changed

- `pr merge` no longer refuses a strategy that the destination branch does not
  list. Nothing can shorten that list: the repository's Merge strategies page
  offers a default and no per-strategy control, and a `restrict_merges` branch
  restriction leaves it alone, so it always names all six. The check could never
  fire, and the only thing it could do was block a merge that would have worked.
- An explicit `--strategy` now costs no extra calls. It is the caller's
  business, so it goes straight through; the two lookups happen only when the
  flag is left off and the destination branch has to be asked.

  Both paths were checked against a repository with its default set to Squash by
  hand, the one thing the API cannot do: no flag produced a one-parent commit,
  and `--strategy merge_commit` produced a two-parent one.

## [1.3.0]

Everything below was run against a throwaway repository with Pipelines enabled
and a pull request open on it, not read off the spec. The spec was wrong about
two of them.

### Fixed

- `pipeline trigger --pull-request` never worked, in any released version. It
  sent a `pipeline_pullrequest_target` carrying only a source branch and an id,
  and Bitbucket answers that with 400 "The request body contains invalid
  properties". The target also needs the destination branch and both commits;
  only its selector is optional. All four are read off the pull request now, so
  the run is a real pull-request run with `BITBUCKET_PR_ID` and
  `BITBUCKET_PR_DESTINATION_BRANCH` set. A `pipeline_ref_target` with a
  `pull-requests` selector is not good enough: it runs the same steps, which is
  why it looks right, but the run has neither variable.
- `pipeline trigger --branch` is refused beside `--pull-request`. Both branches
  come from the pull request, so a run against any other branch is not a
  pull-request run.
- `pipeline trigger --branch` and `pipeline schedules create --branch` defaulted
  to `main`. New repositories in the test workspace are created with a
  `mainbranch` of `master`, so the default was wrong on all of them, and
  `POST pipelines_config/schedules` accepts a branch that does not exist and
  answers 201, so it did not even fail loudly: it created a schedule firing on a
  cron against nothing. Both options now read `mainbranch.name` off the
  repository, or the pull request's own source branch when `--pull-request` is
  set.
- `pipeline trigger --commit` no longer runs without `--branch`. Bitbucket
  accepts a commit that is not on the branch it was given, answers 201 and
  labels the run with that branch, so pairing a real commit with a guessed
  branch was silently wrong rather than rejected.
- `pr merge --strategy` defaulted to `merge_commit` whatever the repository
  said. It now takes the destination branch's own `default_merge_strategy`, and
  a strategy that branch forbids is refused before the call with the allowed
  list quoted. Bitbucket's own answer, "merge_strategy: Select a valid choice",
  does not say what the choices are. The strategies live on
  `GET refs/branches/{name}`; the pull request does not carry them, whatever the
  spec says, and `destination.branch` comes back as `{"name": "master"}` alone.
- `pr merge --strategy` accepts all six strategies Bitbucket allows. The help
  listed three; a live branch reported `merge_commit`, `squash`, `fast_forward`,
  `squash_fast_forward`, `rebase_fast_forward` and `rebase_merge`.
- `pr merge` no longer fails when it cannot read the pull request or the
  destination branch. Those two reads are new, and they are advice: if either
  one fails the merge goes ahead with the strategy that was asked for, as it did
  before they existed.
- `pipeline schedules update` says that a schedule's branch cannot be changed.
  `PUT .../schedules/{uuid}` takes a new `target`, answers 200, echoes the old
  `ref_name` back and moves nothing.

### Documentation

- `--close-source-branch` now says in `--help`, the README and the agent guide
  that it is a server-side field. It closes the branch on Bitbucket and leaves
  the local branch and its remote-tracking ref alone. `gh` spells the nearest
  thing `-d, --delete-branch` and that one does delete locally, so the manual
  cleanup is written out, including why `git branch -d` refuses straight after a
  merge and after a squash merge.
- `pr decline` says that it leaves the source branch open, because the decline
  endpoint takes no body and has no `--close-source-branch` to offer.

## [1.2.0]

### Changed

- `snippet comments --delete` confirms before deleting. It was the only
  destructive verb that fired on sight, and the only comment delete with no
  tombstone to read back afterwards. Scripts calling it must pass `--yes`.

### Added

- `snippet comment-view`, and a `deleted` flag on the three comment listings.

## [1.1.0]

### Changed

- `bbx access` no longer offers `none` as a permission. Bitbucket answers "none
  is not a valid permission"; use the delete verb to clear a grant.
- `bbx repo deploy-keys update` is gone. It cannot succeed: without the key
  Bitbucket calls the key invalid, and with it refuses to change a key's
  contents. Delete and re-add.
- `bbx pipeline oidc` is gone. Only the workspace form of that path exists.

### Fixed

- `pipeline environments changes` posts a `{"change": {...}}` envelope, which is
  what the endpoint takes and what the spec omits entirely.

## [1.0.2]

### Fixed

- Ctrl+C during a command printed `Error: The operation was canceled.` and
  exited 1. A cancelled run is not an error, so it now exits 130, the shell
  convention for SIGINT, and prints nothing. An HttpClient timeout still
  reports as a failure with exit 1.

## [1.0.1]

### Fixed

- Error messages and login prompts pointed at
  `bitbucket.org/account/settings/api-tokens/`, which returns 404. Atlassian
  API tokens are managed on the Atlassian account, so the correct page is
  `id.atlassian.com/manage-profile/security/api-tokens`.

## [1.0.0] first public release

Nothing before this was published to NuGet, so this is the baseline rather
than a break from anything.

### Auth

Atlassian API tokens are the only supported credential. OAuth was built and
then removed before release: Bitbucket's OAuth is bring-your-own-consumer,
so every user would have had to create a private consumer, set a callback
URL and copy a key and secret before logging in once. That is more work than
pasting a token, and it needs workspace admin rights. App passwords are not
supported either; Bitbucket retires them on 2026-06-09.

### Platform

Targets .NET 10, the current LTS, supported until November 2028. .NET 8 and 9
both reach end of support on 10 November 2026, so shipping against them would
have meant a package built for runtimes that expire within months.

### Known gaps

- `bbx workspace list` and `bbx user permissions {workspaces,repositories}`
  return HTTP 410. Atlassian withdrew those endpoints under CHANGE-2770.
- Bitbucket Issues shut down on 2026-08-20; the `issue` group goes with them.

### Included

- Coverage for source files (`src`), downloads, tags, deploy keys, default
  reviewers, branching models, PR tasks, commit statuses, pipeline reports
  and test cases, OIDC, workspace hooks and workspace projects.
- `--json-compact` (and `BBX_JSON_COMPACT=1`) for single-line JSON.
- A test project: 205 tests over a fake HTTP handler, wired into CI.
  xunit v3 and AwesomeAssertions, both current and permissively licensed.

### Fixed before release

- `bbx pipeline logs` returned HTTP 406. Non-JSON GETs now send
  `Accept: */*`; the log endpoint serves `application/octet-stream` and
  refused to fall back.
- `bbx pr diff` and `bbx pr patch` always failed with "You may not have
  access to this repository". Those endpoints redirect, and HttpClient drops
  the `Authorization` header when it follows a redirect. Redirects are now
  followed in-process, re-applying credentials on the same origin only.
- `bbx branch tag list` crashed on a lightweight tag: `TryGetProperty`
  reports success for a JSON `null` and reading through it throws. Fixed
  there and at 41 other nested lookups.
- **Every failed command exited 0.** A value returned from `Main` overrides
  `Environment.ExitCode`.
- 403 responses said only "HTTP 403 Forbidden". They now name the missing
  scope and where to re-issue the token.
- `bbx pr merge` rejected its own default strategy (`merge`; the wire value
  is `merge_commit`).
- `bbx pipeline schedules create` and `bbx branch restrictions add` sent
  payloads Bitbucket rejected.
- `bbx snippet files` used an endpoint that does not exist; `bbx snippet
  watch` crashed on a 204.
- `bbx workspace project default-reviewers|deploy-keys|branching-model` had
  no way to name the project: `--project-key` was bound but never
  registered.
- `pipeline` gained the `-w`/`-r` aliases every other group had.
- Logging in no longer overwrites a default workspace you chose with your
  account name.
