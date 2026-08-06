using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.UpdatePullRequest;

public sealed class UpdatePullRequestHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string NothingToUpdate =
        "Error: nothing to update. Pass at least one of --title, --body, --dest, "
        + "--reviewers, --close-source-branch or --no-close-source-branch.";

    public const string CloseSourceBranchNeedsCompany =
        "Error: Bitbucket ignores a close-source-branch change unless the same call also "
        + "changes something else. Pair it with a --title, --body or --dest that differs "
        + "from the current value.";

    public async Task<JsonElement> HandleAsync(UpdatePullRequestRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        // PUT merges: fields left out keep their current value. So send only
        // what the caller asked to change.
        var body = new Dictionary<string, object>();
        if (request.Title is not null) body["title"] = request.Title;
        if (request.Body is not null) body["description"] = request.Body;
        if (request.Destination is not null)
            body["destination"] = new { branch = new { name = request.Destination } };
        // An absent --reviewers parses to an empty array rather than null, so a
        // null check here would send "reviewers": [] on every edit and strip the
        // reviewers off any pull request that had them. Requiring at least one
        // account ID costs the ability to clear the list, which the CLI has no
        // way to ask for anyway.
        if (request.Reviewers is { Length: > 0 })
            body["reviewers"] = request.Reviewers.Select(r => new { account_id = r }).ToArray();
        if (request.CloseSourceBranch is not null)
            body["close_source_branch"] = request.CloseSourceBranch.Value;

        if (body.Count == 0) throw new BbxUserException(NothingToUpdate);

        var endpoint = $"/repositories/{ws}/{repo}/pullrequests/{request.Id}";

        // Bitbucket only applies close_source_branch when the same PUT moves
        // another field to a new value. On its own, or next to a field being set
        // to what it already holds, it is dropped: the 200 echoes the value back
        // so the call looks like it worked, and a later GET shows the old
        // setting. Rather than report a change that did not happen, read the
        // pull request first and refuse when nothing else would move.
        // Verified against the live API on 2026-08-05.
        if (request.CloseSourceBranch is not null)
        {
            var current = await client.GetAsync<JsonElement>(endpoint, ct);
            if (!ChangesAnotherField(request, current))
                throw new BbxUserException(CloseSourceBranchNeedsCompany);
        }

        return await client.PutAsync<JsonElement>(endpoint, body, ct);
    }

    private static bool ChangesAnotherField(UpdatePullRequestRequest request, JsonElement current)
    {
        if (request.Reviewers is { Length: > 0 }) return true;
        if (request.Title is not null && request.Title != current.GetStringOrNull("title")) return true;
        if (request.Body is not null && request.Body != (current.GetStringOrNull("description") ?? "")) return true;
        if (request.Destination is not null
            && current.TryGetObject("destination", out var destination)
            && destination.TryGetObject("branch", out var branch)
            && request.Destination != branch.GetStringOrNull("name")) return true;
        return false;
    }
}
