# Changelog

All notable changes to `solrevdev.bbx` are documented in this file.
The format loosely follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
this project uses [Conventional Commits](https://www.conventionalcommits.org/),
so commit history is the source of truth for the fine grain.

Versioning follows [Semantic Versioning](https://semver.org/): `MAJOR.MINOR.PATCH`.

## [Unreleased]

(none.)

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
