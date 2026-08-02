# Changelog

All notable changes to `solrevdev.bbx` are documented in this file.
The format loosely follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
this project uses [Conventional Commits](https://www.conventionalcommits.org/),
so commit history is the source of truth for the fine grain.

Versioning follows [Semantic Versioning](https://semver.org/) — `MAJOR.MINOR.PATCH`.

## [Unreleased]

(none.)

## [2.0.0] — first stable release after the OAuth + coverage rewrite

This is the first stable release of `solrevdev.bbx` following the
`feat/oauth-and-api-coverage` work. It is a **breaking release** vs the
unreleased 1.x prototype:

- `bbx auth login --app-password` is gone. Bitbucket retires app
  passwords on **2026-06-09**; the flag and code path are deleted
  rather than carried as a deprecation warning. Use `--oauth`
  (recommended) or `--api-token` (CI / scripted) instead.
- `BbxConfig.AppPassword` and the legacy migration branch in
  `CredentialManager.TryMigrate` are removed. Configs written by any
  v1 prototype that still hold an `AppPassword` field no longer
  auto-migrate; users re-authenticate to rewrite the file in the new
  shape.
- `bbx workspace hooks` moved from the flat-flag form
  (`--view <uuid>` / `--create <url>` / `--delete <uuid>`) to per-verb
  subcommands (`list` / `view` / `create` / `update` / `delete`),
  matching `bbx repo hooks`.
- `bbx workspace projects` (plural) moved from the flat-flag form
  (`--view` / `--create` / `--delete` / `--key`) to per-verb
  subcommands and is now an alias of `bbx workspace project` (singular)
  — both names point at the same subcommand graph covering CRUD plus
  the per-project sub-APIs.

### Added

- **OAuth 2.0 (authorization-code via loopback)** as the primary auth
  method. `bbx auth login --oauth` runs a one-shot `HttpListener` on
  `http://localhost:53682/callback`, opens the user's browser, captures
  the callback, exchanges for tokens, and persists to
  `~/.config/bbx/config.json` (mode 600). Refresh handles rotation with
  a 60-second safety margin and a `SemaphoreSlim` guard.
- **First-run auto-launch**: any authenticated command that finds no
  credentials triggers the OAuth flow itself (skip with
  `BBX_NO_INTERACTIVE=1` or redirected stdin).
- `bbx auth setup-oauth` — print (and optionally open) the OAuth
  consumer-registration walkthrough.
- `bbx auth refresh` — force a token refresh and print the new expiry.
- `bbx auth set-workspace <slug>` — set the default workspace used when
  `-w` is omitted.
- **Endpoint coverage** for: repository webhooks; source / files
  (`bbx src {ls,cat,write}`); tags (`bbx branch tag …`); downloads
  (`bbx download …`); commit build-status create/update; default
  reviewers (repo + project + effective); PR tasks / request-changes /
  commits / patch; repo forks / watchers / branching-model / deploy
  keys; commit filehistory / merge-base / approve / diffstat / reports;
  pipeline reports / test reports / test cases / OIDC config /
  pipeline pull-request triggers; user emails / permissions / view /
  SSH keys; project default reviewers / branching-model / deploy keys;
  workspace hooks per-verb shape.
- `--json-compact` global option (and `BBX_JSON_COMPACT=1` env var) for
  single-line JSON output, ideal for `bbx … | jq -c` pipelines.
- `scripts/smoke.sh` — read-only integration smoke against a sandbox
  workspace.
- `docs/llm-guide.md` — pattern-based reference aimed at code agents
  driving `bbx` non-interactively.

### Changed

- `BitbucketClient` now routes every request through a `SendAsync` core
  and an `IAuthProvider`, so OAuth refresh-and-rotate works without
  touching every command site. `HttpClient` is a DI singleton.
- Project layout moved to feature slices (`Features/<Group>/<Verb>/`
  with request + handler) plus a single
  `Composition/ServiceRegistration.cs`. Each `Commands/*.cs` is
  System.CommandLine wiring only.
- `BbxConfig` gained `AuthMethod`, `AccessToken`, `RefreshToken`,
  `TokenExpiry` (now `DateTimeOffset?`), `OAuthClientId`,
  `OAuthClientSecret`, `ApiToken`. `Username` is the Atlassian email
  for API-token logins.
- Tests: an xUnit project (`tests/Bbx.Tests/`) covers credentials,
  the HTTP client, auth providers, OAuth flow, and per-handler unit
  tests (161 tests at v2.0.0 cut).

### Removed

- `bbx auth login --app-password` flag and the
  `BitbucketClient(string?, string?, string?)` ctor it relied on.
- `BbxConfig.AppPassword` field + legacy migration branch.
- All Phase 0 internal shims (`CredentialManager` static API, the
  `BitbucketClient(BbxConfig)` ctor, per-command `CreateClient` helpers).

### Deferred to a post-publish commit

- `Commands/IssueCommand.cs` + `Features/Issues/*` — Bitbucket shuts
  the Issues API down on **2026-08-20**. The deprecation warning
  shipped earlier stays in place; the command group will be deleted
  in a separate post-publish commit on or just after that date.
- A spike (`spike/oauth-dynamic-port`) to test whether Bitbucket
  accepts variable-port loopback redirects — useful only if the fixed
  port 53682 ever conflicts. See plan §4.2.

[Unreleased]: https://github.com/solrevdev/solrevdev.bbx/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/solrevdev/solrevdev.bbx/releases/tag/v2.0.0
