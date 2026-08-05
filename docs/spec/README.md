# The Bitbucket Cloud API spec

`swagger.json` here is a pinned copy of the spec Atlassian publishes for the
Bitbucket Cloud API v2. It is the document every endpoint decision in
`docs/api-coverage.md` was made against.

| | |
| --- | --- |
| Source | <https://api.bitbucket.org/swagger.json> |
| Fetched | 2026-08-05 |
| SHA-256 | ced73199f584139a75b2f002f707e983b87f6160d5ec64898218cb587f19c1fd |
| Paths | 193 |
| Operations | 331 |

## Why it is here

Two reasons. Answering "what does Bitbucket document for this endpoint" should
not need a network call, and a claim in `docs/api-coverage.md` should be
checkable against the thing it was made from.

## Why it looks different from what Bitbucket serves

Bitbucket serves the spec as a single 921 KB line. Nothing can read that: not
an editor, not `grep`, not an agent. This copy is normalised through `jq -S`,
which pretty prints it and sorts the keys, so it can be searched and so a
refresh produces a readable diff instead of one changed line.

Nothing else is changed. The content is Atlassian's.

## Refreshing it

```bash
scripts/fetch-spec.sh
git diff --stat docs/spec/
```

The script rewrites the table above, so the date and hash always describe the
file next to them. **A stale spec that looks current is worse than no spec**, so
refresh it rather than trusting an old copy, and re-run the coverage check in
`docs/api-coverage.md` if the operation count moves.

## Reading it

It is 34,000 lines, so query it rather than opening it.

```bash
# every operation on one path
jq '.paths["/repositories/{workspace}/{repo_slug}/deploy-keys/{key_id}"]' docs/spec/swagger.json

# what a request body has to look like
jq '.definitions["bitbucket.apps.permissions.serializers.RepoPermissionUpdateSchema"]' docs/spec/swagger.json

# every path mentioning a word
jq -r '.paths | keys[] | select(test("permissions-config"))' docs/spec/swagger.json
```

Two things the spec gets wrong are listed in `CLAUDE.md`, rules 14 to 21. It
documents no body for calls that need one, and marks fields optional that
Bitbucket requires. Treat it as the starting point, not the answer.

## Licence

This file is Atlassian's document, republished here for reference. The MIT
licence covering this repository does not apply to it.

The same spec is served as OpenAPI 3 at
<https://dac-static.atlassian.com/cloud/bitbucket/swagger.v3.json>. The two were
compared on 2026-08-05 and carry the same operations, descriptions, deprecation
flags and request bodies; only the serialisation differs.
