# API coverage decisions

Every Bitbucket Cloud API v2 operation `bbx` did not call, with a verdict for each. The list
comes from diffing the published OpenAPI spec against the endpoints the source calls, run on
2026-08-05 against `https://dac-static.atlassian.com/cloud/bitbucket/swagger.v3.json`.

`docs/spec/swagger.json` is a pinned copy of that spec, so every row below can be checked
without a network call. It is the same document served as Swagger 2.0 rather than OpenAPI 3:
the two carry the same 331 operations, descriptions, deprecation flags and request bodies.
Refresh it with `scripts/fetch-spec.sh`.

| | |
| --- | --- |
| Spec operations | 331 |
| Already covered | 149 |
| Missing | 182 |
| Deprecated in the spec, ignored | 40 |
| Live gaps triaged below | 142 |
| Verdict `cover` | 108 |
| Verdict `skip` | 34 |

The `bbx` command named against a `cover` row is the one that now calls the endpoint.

## Why things are skipped

Four reasons account for every skip.

**Connect app properties (12).** The `properties/{app_key}/{property_name}` endpoints under
repositories, commits, pull requests and users. An app key is issued to a Bitbucket Connect
app. A CLI has no way to obtain one, so these are unreachable rather than merely awkward.

**Pipelines runners (10).** A runner is a registration for a physical machine. Creating one
yields an offline runner that means nothing until a Docker container runs on real hardware.
The workspace-scoped half cannot be aimed at a throwaway repository, so testing it would leave
orphaned registrations in a real workspace.

**Account credential writes (3).** There is no throwaway Bitbucket user, so the only way to
test these is to mutate the real `solrevdev` account keys, which are the ones git
authenticates with. The matching reads are safe, so `GET` on GPG keys stays in scope.

**Already covered or duplicated (3).** Two are false gaps: the script matches on the HTTP verb
helpers it knows, and `CopyToAsync` and `GetRawAsync` are not among them, so it missed
`bbx download get` and `bbx pipeline logs`. One, `POST /snippets`, is the same create that
`bbx snippet create` already performs against the workspace-scoped path.

Five further rows sit outside the Bitbucket surface a CLI drives at all: `addon` and
`hook_events`. They are listed for completeness.

## What the live run changed

The `cover` rows were built, then exercised against a throwaway repository
in the personal `solrevdev` workspace, which was deleted afterwards. Seven
things the documentation gets wrong turned up, and each is now handled:

- A report needs `details`, though the spec marks nothing required.
- Reports, annotations and known hosts need a `type` discriminator. Without one
  the 400 carries no message at all.
- `DELETE pipelines-config/caches` takes `?name=` and clears every cache with
  that name. It is not "clear everything", and answers a bare 400 without it.
- `PUT override-settings` answers 204 with no body.
- `/user/workspaces` returns `workspace_access` records, so the slug sits one
  level down.
- `PUT deploy-keys/{id}` cannot succeed under any body. Delete and re-add.
- `bitbucket.org` is rejected as a pipelines known host, because Bitbucket
  already configures SSH for it.

Two things could not be made to work and are recorded rather than hidden:

- `pr conflicts`, `repo file-conflicts` and the workspace OIDC discovery
  commands answer 403 "This resource does not support authentication using the
  provided token". No scope changes that. `bbx pipeline oidc` used to be listed
  alongside them and was a separate fault: it answered 404 "There is no API
  hosted at this URL", because the spec defines only the workspace form of that
  path. The command has been removed; it never appeared in the table below,
  because the path it called is not in the spec.
- The deployment variables list endpoint answers an empty page even when
  variables exist. Add, update and delete all work.

`pipeline config` and `pipeline ssh key-pair` answer 404 until Pipelines has
been enabled on the repository once; both were confirmed working against a
repository that had.

## The permissions-config writes

Run live on 2026-08-06 against a throwaway repository and a disposable, empty group in the
personal `solrevdev` workspace. Four of the eight are now confirmed end to end:

| Verb | Target | Result |
| --- | --- | --- |
| `PUT`/`DELETE` `permissions-config/groups/{slug}` | repository | Confirmed. `read`, `write` and `admin` all land |
| `PUT`/`DELETE` `permissions-config/groups/{slug}` | project | Confirmed. `create-repo` lands too |
| `PUT`/`DELETE` `permissions-config/users/{id}` | repository | Reachable, not confirmed |
| `PUT`/`DELETE` `permissions-config/users/{id}` | project | Reachable, not confirmed |

The body is `{"permission": "..."}`, as documented. Two things the spec gets wrong:

- It says of all eight "The only authentication method for this endpoint is via app
  passwords". That is stale. App passwords were withdrawn on 28 July 2026, and an API
  token drives these fine.
- `none` is not a permission. All four `set` verbs offered it; Bitbucket answers 400
  "none is not a valid permission". It has been dropped, and `remove` does that job.

The user-keyed pair stays unconfirmed because the target must be a workspace member and
cannot be the workspace owner, and `solrevdev` has one member. Aimed at the owner they
answer 400 "This user is linked to this workspace, so their access on any of the
repositories cannot be modified or removed", which proves the path resolves and the body
parses but not that a grant lands.

## Repository

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `PUT` | `/repositories/{workspace}/{repo_slug}` | cover | `bbx repo update` |

## Pull requests

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/repositories/{workspace}/{repo_slug}/pullrequests/activity` | cover | `bbx pr activity (id now optional)` |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/pullrequests/{pull_request_id}/comments/{comment_id}` | cover | `bbx pr comment-delete` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pullrequests/{pull_request_id}/comments/{comment_id}` | cover | `bbx pr comment-view` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/pullrequests/{pull_request_id}/comments/{comment_id}` | cover | `bbx pr comment-update` |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/pullrequests/{pull_request_id}/comments/{comment_id}/resolve` | cover | `bbx pr comment-unresolve` |
| `POST` | `/repositories/{workspace}/{repo_slug}/pullrequests/{pull_request_id}/comments/{comment_id}/resolve` | cover | `bbx pr comment-resolve` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pullrequests/{pull_request_id}/conflicts` | cover | `bbx pr conflicts` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pullrequests/{pull_request_id}/diffstat` | cover | `bbx pr diffstat` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pullrequests/{pull_request_id}/merge/task-status/{task_id}` | cover | `bbx pr merge-status` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pullrequests/{pull_request_id}/tasks/{task_id}` | cover | `bbx pr tasks view` |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/pullrequests/{pullrequest_id}/properties/{app_key}/{property_name}` | skip | Connect app property; app key unobtainable from a CLI |
| `GET` | `/repositories/{workspace}/{repo_slug}/pullrequests/{pullrequest_id}/properties/{app_key}/{property_name}` | skip | Connect app property; app key unobtainable from a CLI |
| `PUT` | `/repositories/{workspace}/{repo_slug}/pullrequests/{pullrequest_id}/properties/{app_key}/{property_name}` | skip | Connect app property; app key unobtainable from a CLI |

## Commit comments, reports and statuses

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `POST` | `/repositories/{workspace}/{repo_slug}/commit/{commit}/comments` | cover | `bbx commit comment` |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/commit/{commit}/comments/{comment_id}` | cover | `bbx commit comment-delete` |
| `GET` | `/repositories/{workspace}/{repo_slug}/commit/{commit}/comments/{comment_id}` | cover | `bbx commit comment-view` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/commit/{commit}/comments/{comment_id}` | cover | `bbx commit comment-update` |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/commit/{commit}/properties/{app_key}/{property_name}` | skip | Connect app property; app key unobtainable from a CLI |
| `GET` | `/repositories/{workspace}/{repo_slug}/commit/{commit}/properties/{app_key}/{property_name}` | skip | Connect app property; app key unobtainable from a CLI |
| `PUT` | `/repositories/{workspace}/{repo_slug}/commit/{commit}/properties/{app_key}/{property_name}` | skip | Connect app property; app key unobtainable from a CLI |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/commit/{commit}/reports/{reportId}` | cover | `bbx pipeline reports delete` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/commit/{commit}/reports/{reportId}` | cover | `bbx pipeline reports update` |
| `POST` | `/repositories/{workspace}/{repo_slug}/commit/{commit}/reports/{reportId}/annotations` | cover | `bbx pipeline reports annotations-create` |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/commit/{commit}/reports/{reportId}/annotations/{annotationId}` | cover | `bbx pipeline reports annotation-delete` |
| `GET` | `/repositories/{workspace}/{repo_slug}/commit/{commit}/reports/{reportId}/annotations/{annotationId}` | cover | `bbx pipeline reports annotation-view` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/commit/{commit}/reports/{reportId}/annotations/{annotationId}` | cover | `bbx pipeline reports annotation-update` |
| `GET` | `/repositories/{workspace}/{repo_slug}/commit/{commit}/statuses/build/{key}` | cover | `bbx commit status view` |

## Commit listing

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `POST` | `/repositories/{workspace}/{repo_slug}/commits` | cover | `bbx commit list --include/--exclude` |
| `POST` | `/repositories/{workspace}/{repo_slug}/commits/{revision}` | cover | `bbx commit list <revision> --include/--exclude` |

## Branch restrictions

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/repositories/{workspace}/{repo_slug}/branch-restrictions/{id}` | cover | `bbx branch restrictions view` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/branch-restrictions/{id}` | cover | `bbx branch restrictions update` |

## Repository default reviewers

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/repositories/{workspace}/{repo_slug}/default-reviewers/{target_username}` | cover | `bbx repo default-reviewers view` |

## Repository deploy keys

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `PUT` | `/repositories/{workspace}/{repo_slug}/deploy-keys/{key_id}` | skip | Cannot succeed. Demands `key`, then refuses it whatever the value. Eight bodies tried live on 2026-08-06; the command was removed |

## Effective branching model

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/repositories/{workspace}/{repo_slug}/effective-branching-model` | cover | `bbx repo branching-model effective` |

## Repository setting inheritance

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/repositories/{workspace}/{repo_slug}/override-settings` | cover | `bbx repo override-settings view` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/override-settings` | cover | `bbx repo override-settings update` |

## File conflicts

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/repositories/{workspace}/{repo_slug}/file-conflicts/{spec}` | cover | `bbx repo file-conflicts` |

## Refs

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/repositories/{workspace}/{repo_slug}/refs` | cover | `bbx branch refs` |

## Source

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/repositories/{workspace}/{repo_slug}/src` | cover | `bbx src ls with --ref omitted` |

## Downloads

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/repositories/{workspace}/{repo_slug}/downloads/{filename}` | skip | False gap: bbx download get already calls it (CopyToAsync, which the script does not match) |

## Repository access

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/repositories/{workspace}/{repo_slug}/permissions-config/groups` | cover | `bbx repo access groups list` |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/permissions-config/groups/{group_slug}` | cover | `bbx repo access groups remove (unit tests only)` |
| `GET` | `/repositories/{workspace}/{repo_slug}/permissions-config/groups/{group_slug}` | cover | `bbx repo access groups view` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/permissions-config/groups/{group_slug}` | cover | `bbx repo access groups set (unit tests only, needs a second account or a group)` |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/permissions-config/users/{selected_user_id}` | cover | `bbx repo access users remove (unit tests only)` |
| `GET` | `/repositories/{workspace}/{repo_slug}/permissions-config/users/{selected_user_id}` | cover | `bbx repo access users view` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/permissions-config/users/{selected_user_id}` | cover | `bbx repo access users set (unit tests only)` |

## Pipeline steps and logs

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/repositories/{workspace}/{repo_slug}/pipelines/{pipeline_uuid}/steps/{step_uuid}` | cover | `bbx pipeline step` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pipelines/{pipeline_uuid}/steps/{step_uuid}/log` | skip | False gap: bbx pipeline logs already calls it (GetRawAsync, which the script does not match) |
| `GET` | `/repositories/{workspace}/{repo_slug}/pipelines/{pipeline_uuid}/steps/{step_uuid}/logs/{log_uuid}` | cover | `bbx pipeline logs --log-uuid` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pipelines/{pipeline_uuid}/steps/{step_uuid}/test_reports/test_cases/{test_case_uuid}/test_case_reasons` | cover | `bbx pipeline test-case-reasons` |

## Pipeline caches and runners

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/pipelines-config/caches` | cover | `bbx pipeline caches clear --all` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pipelines-config/caches/{cache_uuid}/content-uri` | cover | `bbx pipeline caches content-uri` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pipelines-config/runners` | skip | Self-hosted runner registration; needs real hardware and a Docker container to mean anything |
| `POST` | `/repositories/{workspace}/{repo_slug}/pipelines-config/runners` | skip | Self-hosted runner registration; needs real hardware and a Docker container to mean anything |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/pipelines-config/runners/{runner_uuid}` | skip | Self-hosted runner registration; needs real hardware and a Docker container to mean anything |
| `GET` | `/repositories/{workspace}/{repo_slug}/pipelines-config/runners/{runner_uuid}` | skip | Self-hosted runner registration; needs real hardware and a Docker container to mean anything |
| `PUT` | `/repositories/{workspace}/{repo_slug}/pipelines-config/runners/{runner_uuid}` | skip | Self-hosted runner registration; needs real hardware and a Docker container to mean anything |

## Pipeline configuration, schedules, SSH and variables

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/repositories/{workspace}/{repo_slug}/pipelines_config` | cover | `bbx pipeline config view` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/pipelines_config` | cover | `bbx pipeline config update` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/pipelines_config/build_number` | cover | `bbx pipeline config build-number` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pipelines_config/schedules/{schedule_uuid}` | cover | `bbx pipeline schedules view` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/pipelines_config/schedules/{schedule_uuid}` | cover | `bbx pipeline schedules update` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pipelines_config/schedules/{schedule_uuid}/executions` | cover | `bbx pipeline schedules executions` |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/pipelines_config/ssh/key_pair` | cover | `bbx pipeline ssh key-pair delete` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pipelines_config/ssh/key_pair` | cover | `bbx pipeline ssh key-pair view` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/pipelines_config/ssh/key_pair` | cover | `bbx pipeline ssh key-pair set` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pipelines_config/ssh/known_hosts` | cover | `bbx pipeline ssh known-hosts list` |
| `POST` | `/repositories/{workspace}/{repo_slug}/pipelines_config/ssh/known_hosts` | cover | `bbx pipeline ssh known-hosts add` |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/pipelines_config/ssh/known_hosts/{known_host_uuid}` | cover | `bbx pipeline ssh known-hosts delete` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pipelines_config/ssh/known_hosts/{known_host_uuid}` | cover | `bbx pipeline ssh known-hosts view` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/pipelines_config/ssh/known_hosts/{known_host_uuid}` | cover | `bbx pipeline ssh known-hosts update` |
| `GET` | `/repositories/{workspace}/{repo_slug}/pipelines_config/variables/{variable_uuid}` | cover | `bbx pipeline variables view` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/pipelines_config/variables/{variable_uuid}` | cover | `bbx pipeline variables update` |

## Deployments

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/repositories/{workspace}/{repo_slug}/deployments` | cover | `bbx pipeline deploys list` |
| `GET` | `/repositories/{workspace}/{repo_slug}/deployments/{deployment_uuid}` | cover | `bbx pipeline deploys view` |

## Deployment environments

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `POST` | `/repositories/{workspace}/{repo_slug}/environments` | cover | `bbx pipeline deployments create` |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/environments/{environment_uuid}` | cover | `bbx pipeline deployments delete` |
| `POST` | `/repositories/{workspace}/{repo_slug}/environments/{environment_uuid}/changes` | cover | `bbx pipeline deployments changes`, confirmed live 2026-08-06 |

## Deployment variables

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/repositories/{workspace}/{repo_slug}/deployments_config/environments/{environment_uuid}/variables` | cover | `bbx pipeline deployments variables list` |
| `POST` | `/repositories/{workspace}/{repo_slug}/deployments_config/environments/{environment_uuid}/variables` | cover | `bbx pipeline deployments variables add` |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/deployments_config/environments/{environment_uuid}/variables/{variable_uuid}` | cover | `bbx pipeline deployments variables delete` |
| `PUT` | `/repositories/{workspace}/{repo_slug}/deployments_config/environments/{environment_uuid}/variables/{variable_uuid}` | cover | `bbx pipeline deployments variables update` |

## Repository application properties

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `DELETE` | `/repositories/{workspace}/{repo_slug}/properties/{app_key}/{property_name}` | skip | Connect app property; app key unobtainable from a CLI |
| `GET` | `/repositories/{workspace}/{repo_slug}/properties/{app_key}/{property_name}` | skip | Connect app property; app key unobtainable from a CLI |
| `PUT` | `/repositories/{workspace}/{repo_slug}/properties/{app_key}/{property_name}` | skip | Connect app property; app key unobtainable from a CLI |

## Snippets

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `POST` | `/snippets` | skip | Duplicate of the workspace-scoped create that bbx snippet create already uses |
| `PUT` | `/snippets/{workspace}/{encoded_id}/comments/{comment_id}` | cover | `bbx snippet comments update` |
| `GET` | `/snippets/{workspace}/{encoded_id}/commits` | cover | `bbx snippet commits` |
| `GET` | `/snippets/{workspace}/{encoded_id}/commits/{revision}` | cover | `bbx snippet commits --revision` |
| `DELETE` | `/snippets/{workspace}/{encoded_id}/{node_id}` | cover | `bbx snippet delete --revision` |
| `GET` | `/snippets/{workspace}/{encoded_id}/{node_id}` | cover | `bbx snippet view --revision` |
| `PUT` | `/snippets/{workspace}/{encoded_id}/{node_id}` | cover | `bbx snippet update --revision` |
| `GET` | `/snippets/{workspace}/{encoded_id}/{node_id}/files/{path}` | cover | `bbx snippet files --revision` |
| `GET` | `/snippets/{workspace}/{encoded_id}/{revision}/diff` | cover | `bbx snippet diff` |
| `GET` | `/snippets/{workspace}/{encoded_id}/{revision}/patch` | cover | `bbx snippet patch` |

## The authenticated account

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/user/emails/{email}` | cover | `bbx user emails --email` |
| `GET` | `/user/workspaces` | cover | `bbx user workspaces` |
| `GET` | `/user/workspaces/{workspace}/permission` | cover | `bbx user permissions workspace` |
| `GET` | `/user/workspaces/{workspace}/permissions/repositories` | cover | `bbx user permissions repositories --workspace` |

## User accounts

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/users/{selected_user}/gpg-keys` | cover | `bbx user gpg-keys list` |
| `POST` | `/users/{selected_user}/gpg-keys` | skip | Writes to the only real account on this machine; no throwaway user exists |
| `DELETE` | `/users/{selected_user}/gpg-keys/{fingerprint}` | skip | Writes to the only real account on this machine; no throwaway user exists |
| `GET` | `/users/{selected_user}/gpg-keys/{fingerprint}` | cover | `bbx user gpg-keys view` |
| `DELETE` | `/users/{selected_user}/properties/{app_key}/{property_name}` | skip | Connect app property; app key unobtainable from a CLI |
| `GET` | `/users/{selected_user}/properties/{app_key}/{property_name}` | skip | Connect app property; app key unobtainable from a CLI |
| `PUT` | `/users/{selected_user}/properties/{app_key}/{property_name}` | skip | Connect app property; app key unobtainable from a CLI |
| `PUT` | `/users/{selected_user}/ssh-keys/{key_id}` | skip | Would rewrite the SSH key git authenticates with; add and delete already exist |

## Workspace members

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/workspaces/{workspace}/members/{member}` | cover | `bbx workspace member` |

## Workspace permissions

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/workspaces/{workspace}/permissions/repositories` | cover | `bbx workspace repo-permissions` |
| `GET` | `/workspaces/{workspace}/permissions/repositories/{repo_slug}` | cover | `bbx workspace repo-permissions --repo` |

## Workspace settings

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/workspaces/{workspace}/settings/gpg/public-key` | cover | `bbx workspace gpg-key` |

## Workspace pull requests

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/workspaces/{workspace}/pullrequests/{selected_user}` | cover | `bbx workspace pullrequests` |

## Workspace pipeline configuration

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/workspaces/{workspace}/pipelines-config/identity/oidc/.well-known/openid-configuration` | cover | `bbx workspace pipelines oidc config` |
| `GET` | `/workspaces/{workspace}/pipelines-config/identity/oidc/keys.json` | cover | `bbx workspace pipelines oidc keys` |
| `GET` | `/workspaces/{workspace}/pipelines-config/runners` | skip | Self-hosted runner registration, workspace-scoped; cannot be aimed at a throwaway repository |
| `POST` | `/workspaces/{workspace}/pipelines-config/runners` | skip | Self-hosted runner registration, workspace-scoped; cannot be aimed at a throwaway repository |
| `DELETE` | `/workspaces/{workspace}/pipelines-config/runners/{runner_uuid}` | skip | Self-hosted runner registration, workspace-scoped; cannot be aimed at a throwaway repository |
| `GET` | `/workspaces/{workspace}/pipelines-config/runners/{runner_uuid}` | skip | Self-hosted runner registration, workspace-scoped; cannot be aimed at a throwaway repository |
| `PUT` | `/workspaces/{workspace}/pipelines-config/runners/{runner_uuid}` | skip | Self-hosted runner registration, workspace-scoped; cannot be aimed at a throwaway repository |
| `GET` | `/workspaces/{workspace}/pipelines-config/variables` | cover | `bbx workspace pipelines variables list` |
| `POST` | `/workspaces/{workspace}/pipelines-config/variables` | cover | `bbx workspace pipelines variables add` |
| `DELETE` | `/workspaces/{workspace}/pipelines-config/variables/{variable_uuid}` | cover | `bbx workspace pipelines variables delete` |
| `GET` | `/workspaces/{workspace}/pipelines-config/variables/{variable_uuid}` | cover | `bbx workspace pipelines variables view` |
| `PUT` | `/workspaces/{workspace}/pipelines-config/variables/{variable_uuid}` | cover | `bbx workspace pipelines variables update` |

## Workspace projects

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `PUT` | `/workspaces/{workspace}/projects/{project_key}` | cover | `bbx workspace project update` |
| `GET` | `/workspaces/{workspace}/projects/{project_key}/branching-model/settings` | cover | `bbx workspace project branching-model settings` |
| `GET` | `/workspaces/{workspace}/projects/{project_key}/default-reviewers/{selected_user}` | cover | `bbx workspace project default-reviewers view` |
| `GET` | `/workspaces/{workspace}/projects/{project_key}/permissions-config/groups` | cover | `bbx workspace project access groups list` |
| `DELETE` | `/workspaces/{workspace}/projects/{project_key}/permissions-config/groups/{group_slug}` | cover | `bbx workspace project access groups remove (unit tests only)` |
| `GET` | `/workspaces/{workspace}/projects/{project_key}/permissions-config/groups/{group_slug}` | cover | `bbx workspace project access groups view` |
| `PUT` | `/workspaces/{workspace}/projects/{project_key}/permissions-config/groups/{group_slug}` | cover | `bbx workspace project access groups set (unit tests only)` |
| `GET` | `/workspaces/{workspace}/projects/{project_key}/permissions-config/users` | cover | `bbx workspace project access users list` |
| `DELETE` | `/workspaces/{workspace}/projects/{project_key}/permissions-config/users/{selected_user_id}` | cover | `bbx workspace project access users remove (unit tests only)` |
| `GET` | `/workspaces/{workspace}/projects/{project_key}/permissions-config/users/{selected_user_id}` | cover | `bbx workspace project access users view` |
| `PUT` | `/workspaces/{workspace}/projects/{project_key}/permissions-config/users/{selected_user_id}` | cover | `bbx workspace project access users set (unit tests only)` |

## Connect apps

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `DELETE` | `/addon` | skip | Connect app only; no CLI can hold an app key |
| `PUT` | `/addon` | skip | Connect app only; no CLI can hold an app key |
| `GET` | `/addon/{addon_key}/client-key` | skip | Connect app only |

## Webhook event metadata

| Method | Path | Verdict | Command or reason |
| --- | --- | --- | --- |
| `GET` | `/hook_events` | skip | Connect-oriented metadata; long dead |
| `GET` | `/hook_events/{subject_type}` | skip | Connect-oriented metadata; long dead |
