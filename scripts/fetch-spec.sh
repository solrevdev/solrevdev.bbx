#!/usr/bin/env bash
# scripts/fetch-spec.sh — refresh the pinned copy of the Bitbucket Cloud API
# spec at docs/spec/swagger.json.
#
# Bitbucket serves the spec as a single 921 KB line, which no editor, grep or
# agent can read. This normalises it through `jq -S`, so the file can be
# searched and so a refresh produces a readable diff rather than one changed
# line. Sorted keys mean the diff shows what Bitbucket changed, not how its
# generator happened to order things that day.
#
# The script also rewrites the provenance lines in docs/spec/README.md, so the
# date and hash beside the file always describe the file.
#
# Requirements: curl, jq, shasum, python3.
#
# Usage:
#   scripts/fetch-spec.sh
#   git diff --stat docs/spec/    # see what Bitbucket changed

set -euo pipefail

URL="https://api.bitbucket.org/swagger.json"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="$ROOT/docs/spec/swagger.json"
NOTE="$ROOT/docs/spec/README.md"

tmp="$(mktemp)"
trap 'rm -f "$tmp"' EXIT

echo "Fetching $URL"
curl -fsSL "$URL" | jq -S . > "$tmp"
mv "$tmp" "$OUT"
chmod 644 "$OUT"   # mktemp creates 0600, which git would record as a mode change

fetched="$(date -u +%Y-%m-%d)"
sha="$(shasum -a 256 "$OUT" | cut -d ' ' -f 1)"
paths="$(jq '.paths | length' "$OUT")"
ops="$(jq '[.paths[] | keys[] | select(. == "get" or . == "post" or . == "put" or . == "delete" or . == "patch")] | length' "$OUT")"

FETCHED="$fetched" SHA="$sha" PATHS="$paths" OPS="$ops" NOTE="$NOTE" python3 - <<'PY'
import os, re

note = os.environ["NOTE"]
fields = {
    "Fetched": os.environ["FETCHED"],
    "SHA-256": os.environ["SHA"],
    "Paths": os.environ["PATHS"],
    "Operations": os.environ["OPS"],
}

text = open(note).read()
for name, value in fields.items():
    text, n = re.subn(rf"^\| {name} \| .* \|$", f"| {name} | {value} |", text, flags=re.M)
    if n != 1:
        raise SystemExit(f"docs/spec/README.md has no '| {name} |' row to update")
open(note, "w").write(text)
PY

echo "Wrote $OUT"
echo "  fetched    $fetched"
echo "  sha-256    $sha"
echo "  paths      $paths"
echo "  operations $ops"
