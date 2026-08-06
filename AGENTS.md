# AGENTS.md

Instructions for coding agents working in this repository. Human contributors
should read [CONTRIBUTING](README.md#contributing) in the README; the rules below
are the same ones, stated for automation.

## Build and verify

```bash
dotnet build src/Bbx/Bbx.csproj                 # net10.0
dotnet test tests/Bbx.Tests/Bbx.Tests.csproj    # must pass before you commit
dotnet run --project src/Bbx/Bbx.csproj -f net10.0 -- <args>
```

Tests use a fake HTTP handler and never touch the network. Do not add a test
that needs credentials.

## Where code goes

| Put this | Here |
| --- | --- |
| Argument parsing, option definitions | `src/Bbx/Commands/<Area>Command.cs` |
| Business logic, endpoint calls, shaping | `src/Bbx/Features/<Area>/<Verb>/` |
| HTTP behaviour shared by everything | `src/Bbx/Api/BitbucketClient.cs` |
| DI registration | `src/Bbx/Composition/ServiceRegistration.cs` |

A `Commands/` file that talks to `BitbucketClient` directly is in the wrong
place.

## Invariants

Break these and something fails in production but not in the tests:

- Nested JSON is read with `TryGetObject`. `TryGetProperty` reports success for
  a JSON `null`, and reading through it throws.
- Endpoints are relative and take no leading slash.
- Non-JSON GETs send `Accept: */*`; `application/json` gets a 406 from the
  pipeline log endpoint.
- Redirects are followed by `BitbucketClient`, not `HttpClient`, so the
  `Authorization` header survives. Same-origin only.
- Failure exits non-zero. Do not return a value from `Main` that discards
  `Environment.ExitCode`.
- Destructive verbs take `--yes` and confirm otherwise.
- New behaviour ships with a test.

## The API spec

`docs/spec/swagger.json` is a pinned copy of the Bitbucket Cloud spec. Read it
before guessing at an endpoint, and do not fetch the documentation site:

```bash
jq '.paths["/repositories/{workspace}/{repo_slug}/deploy-keys/{key_id}"]' docs/spec/swagger.json
```

`scripts/fetch-spec.sh` refreshes it and records the date beside it. The spec is
often wrong about request bodies and required fields, so confirm anything it
tells you against a real call before writing it down as fact.

## Live testing

If you test against real Bitbucket:

- Read-only commands may run against real repositories.
- Anything that creates, updates or deletes runs against a throwaway repository
  you create and delete in the same session. Never mutate a live repository.
- Verify the teardown actually happened before you report success.

## Auth

API tokens only; OAuth was removed. A token is Basic auth (`email:token`) stored
at `~/.config/bbx/config.json`. Set `BBX_NO_INTERACTIVE=1` so a missing
credential fails fast instead of prompting.

Some endpoints need scopes a token may not carry. A 403 names the missing scope.
Report that to the user rather than working around it.

## Commits

Conventional Commits. Explain why in the body, not just what:

```
fix(api): follow redirects with auth

HttpClient drops Authorization when it follows a redirect, so pr diff and
pr patch came back as "You may not have access to this repository".
```

Mark breaking changes with `!` and a `BREAKING CHANGE:` footer.
