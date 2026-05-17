# Bitbucket Cloud Issues and Wikis Sunset

> **Source:** Email from Atlassian to john@solrevdev.com, dated 25 March 2026
> **Subject:** Bitbucket Cloud Issues and Wikis are being removed
> **Affected workspace:** `foremost-group`

## Summary

Atlassian is sunsetting Bitbucket Cloud's native **Issues** and **Wikis** features to focus on Jira Software and Confluence. Repositories in the `foremost-group` workspace currently use one or both of these features.

## Key Dates

| Date | What happens |
|---|---|
| **April 2026** | Issues and Wikis can no longer be enabled on repositories that don't already use them |
| **August 20, 2026** | All existing Issues and Wikis — including related API endpoints — are permanently removed from Bitbucket Cloud |

## Impact on bbx CLI Tool

The entire `bbx issue` command group (`list`, `view`, `create`, `update`, `delete`, `comments`, `comment`) calls the Bitbucket Cloud REST API v2.0 Issues endpoints. These endpoints will **stop working on August 20, 2026**.

### Affected API Endpoints

| bbx command | HTTP method | API endpoint | Status |
|---|---|---|---|
| `bbx issue list` | GET | `/repositories/{workspace}/{repo}/issues` | Will be removed |
| `bbx issue view` | GET | `/repositories/{workspace}/{repo}/issues/{id}` | Will be removed |
| `bbx issue create` | POST | `/repositories/{workspace}/{repo}/issues` | Will be removed |
| `bbx issue update` | PUT | `/repositories/{workspace}/{repo}/issues/{id}` | Will be removed |
| `bbx issue delete` | DELETE | `/repositories/{workspace}/{repo}/issues/{id}` | Will be removed |
| `bbx issue comments` | GET | `/repositories/{workspace}/{repo}/issues/{id}/comments` | Will be removed |
| `bbx issue comment` | POST | `/repositories/{workspace}/{repo}/issues/{id}/comments` | Will be removed |

### Unaffected Commands

All other bbx commands are unaffected: `repo`, `pr`, `branch`, `commit`, `pipeline`, `snippet`, `workspace`, `auth`.

## Recommended Actions

### For bbx users

1. **Export issue data** before August 20, 2026
2. **Migrate issues to Jira Software** if continued tracking is needed
3. **Clone wikis locally** (`git clone`) and migrate content to Confluence

### For bbx development

1. Deprecation warnings have been added to all `bbx issue *` subcommands (as of March 2026)
2. The `IssueCommand` should be removed or replaced after August 20, 2026
3. See [`oauth-and-api-coverage.md`](./oauth-and-api-coverage.md) §5.3 for
   the policy decision: no new investment in `bbx issue *` endpoints; the
   API surface budget goes to webhooks, source/files, default reviewers,
   build statuses, and the rest of §5.1.

## Official Atlassian Resources

- [Export or import issue data](https://support.atlassian.com/bitbucket-cloud/docs/export-or-import-issue-data/)
- [Clone a wiki](https://support.atlassian.com/bitbucket-cloud/docs/clone-a-wiki/)
- [Connect Bitbucket Cloud to Jira Software Cloud](https://support.atlassian.com/bitbucket-cloud/docs/connect-bitbucket-cloud-to-jira-software-cloud/)
- [Full sunset announcement (Atlassian Community)](https://community.atlassian.com/forums/Bitbucket-articles/Announcing-sunset-of-Bitbucket-Issues-and-Wikis/ba-p/3193882)
- [Atlassian Support](https://support.atlassian.com/contact/#/?inquiry_category=technical_issues&product_key=com-atlassian-bitbucket&is_cloud=true&support_type=customer)
- [Bitbucket Community Forum](https://community.atlassian.com/forums/Bitbucket/ct-p/bitbucket)
