# solrevdev.bbx

`gh` for Bitbucket Cloud. A .NET global tool that puts the Bitbucket Cloud API v2
on your command line, with JSON on stdout so scripts and LLMs can read it.

## Install

```
dotnet tool install -g solrevdev.bbx
```

## Authenticate

Create an Atlassian API token at
<https://bitbucket.org/account/settings/api-tokens/>, then:

```
bbx auth login
bbx auth set-workspace myworkspace
```

Credentials are stored in `~/.config/bbx/config.json` with mode 0600.

## Use

```
bbx repo list -w myworkspace --limit 10
bbx pr list -r myrepo --state OPEN
bbx pr view 42 -r myrepo
bbx pr diff 42 -r myrepo
bbx pipeline logs '{pipeline-uuid}' '{step-uuid}' -r myrepo
bbx src cat --ref main README.md -r myrepo
```

Command groups: `auth`, `repo`, `pr`, `branch`, `commit`, `src`, `download`,
`issue`, `pipeline`, `snippet`, `workspace`, `user`.

Run `bbx <group> --help` for the full surface.

## Output contract

- JSON on stdout, prompts and errors on stderr, so pipes stay clean.
- Exit `0` on success, `1` on any failure.
- `--json-compact` (or `BBX_JSON_COMPACT=1`) for single-line JSON.
- `BBX_NO_INTERACTIVE=1` to guarantee no prompting, for CI.

```
bbx pr list -r myrepo --state OPEN | jq -r '.pull_requests[].title'
```

A few commands print raw text because that is the useful form: `pr diff`,
`pr patch`, `commit diff`, `commit patch`, `src cat`.

## Scopes

Token scopes are chosen when you create the token. A call that needs a scope you
did not grant returns a 403 naming the gap:

```
Error: Your credentials lack one or more required privilege scopes.
(HTTP 403 Forbidden) Missing token scopes: admin:repository:bitbucket.
Re-issue your token with those scopes at
https://bitbucket.org/account/settings/api-tokens/
```

## Notes

- Atlassian withdrew the cross-workspace discovery endpoints (CHANGE-2770), so
  `bbx workspace list` and `bbx user permissions ...` return HTTP 410. Name the
  workspace, or set a default with `bbx auth set-workspace`.
- Bitbucket Issues shut down on 2026-08-20; the `issue` group goes with them.

Full documentation, recipes and troubleshooting:
<https://github.com/solrevdev/solrevdev.bbx>

MIT licensed.
