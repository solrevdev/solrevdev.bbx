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
```

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
