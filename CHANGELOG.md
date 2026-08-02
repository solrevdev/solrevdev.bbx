# Changelog

All notable changes to `solrevdev.bbx` are documented in this file.
The format loosely follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
this project uses [Conventional Commits](https://www.conventionalcommits.org/),
so commit history is the source of truth for the fine grain.

Versioning follows [Semantic Versioning](https://semver.org/) — `MAJOR.MINOR.PATCH`.

## [Unreleased]

(none.)

## [2.0.0] — API tokens, wider coverage, a test suite

First stable release. Breaking against the unreleased 1.x prototype.

### Removed

- **OAuth.** `bbx auth login --oauth`, `bbx auth setup-oauth` and
  `bbx auth refresh` are gone, along with the stored consumer and tokens.
  Bitbucket's OAuth is bring-your-own-consumer: every user had to create a
  private consumer, set a callback URL and copy a key and secret before
  logging in once. That is more work than pasting an API token and needs
  workspace admin rights. Anyone who logged in with OAuth must run
  `bbx auth login` again.
- **App passwords.** Bitbucket retires them on 2026-06-09.
- `bbx workspace list` and `bbx user permissions {workspaces,repositories}`
  still exist but return HTTP 410: Atlassian withdrew the underlying
  endpoints under CHANGE-2770.

### Added

- Coverage for source files (`src`), downloads, tags, deploy keys, default
  reviewers, branching models, PR tasks, commit statuses, pipeline reports
  and test cases, OIDC, workspace hooks and workspace projects.
- `--json-compact` (and `BBX_JSON_COMPACT=1`) for single-line JSON.
- A test project: 190 tests over a fake HTTP handler, wired into CI.

### Fixed

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
