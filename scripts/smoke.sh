#!/usr/bin/env bash
# scripts/smoke.sh — read-only integration smoke for bbx against a real
# Bitbucket workspace. Safe to run against any sandbox: no mutations.
#
# Requirements:
#   - `bbx` on PATH (either `dotnet tool install -g solrevdev.bbx` or
#     `dotnet run --project src/Bbx/Bbx.csproj -f net10.0 --` aliased).
#   - Stored API-token credentials (`bbx auth status` must succeed). This
#     script will not prompt; run `bbx auth login` first.
#   - `jq` for quick output sanity checks.
#
# Environment:
#   BBX_SMOKE_WORKSPACE  Bitbucket workspace slug (required).
#   BBX_SMOKE_REPO       Repository slug inside that workspace (required).
#   BBX_SMOKE_REF        Optional ref for `src ls` (default: main).
#   BBX                  Override the bbx command (default: `bbx`).
#
# Each step exits non-zero on failure thanks to `set -euo pipefail`.
# No CI wiring — this script is meant to be run by hand before tagging
# a release.

set -euo pipefail

: "${BBX_SMOKE_WORKSPACE:?Set BBX_SMOKE_WORKSPACE to a workspace slug}"
: "${BBX_SMOKE_REPO:?Set BBX_SMOKE_REPO to a repo slug inside that workspace}"
BBX_SMOKE_REF="${BBX_SMOKE_REF:-main}"
BBX="${BBX:-bbx}"

step() { printf '\n=== %s ===\n' "$*"; }

step "bbx auth status"
# auth status prints plain text (not JSON) and exits non-zero if not authenticated.
"$BBX" auth status >/dev/null

step "bbx repo list (limit 5)"
"$BBX" repo list -w "$BBX_SMOKE_WORKSPACE" --limit 5 | jq -e '.repositories | length >= 0' >/dev/null

step "bbx pr list --state OPEN (limit 5)"
"$BBX" pr list -w "$BBX_SMOKE_WORKSPACE" -r "$BBX_SMOKE_REPO" --state OPEN --limit 5 \
    | jq -e '.pull_requests | length >= 0' >/dev/null

step "bbx src ls --ref $BBX_SMOKE_REF (root)"
"$BBX" src ls -w "$BBX_SMOKE_WORKSPACE" -r "$BBX_SMOKE_REPO" --ref "$BBX_SMOKE_REF" \
    | jq -e '.entries | length >= 0' >/dev/null

step "bbx repo hooks list"
"$BBX" repo hooks list -w "$BBX_SMOKE_WORKSPACE" -r "$BBX_SMOKE_REPO" \
    | jq -e '.hooks | length >= 0' >/dev/null

printf '\nAll smoke checks passed.\n'
