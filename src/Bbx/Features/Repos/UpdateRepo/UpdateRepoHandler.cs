using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.UpdateRepo;

public sealed class UpdateRepoHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string NothingToUpdate =
        "Error: nothing to update. Pass at least one of --name, --description, --private, "
        + "--public, --fork-policy, --language, --website, --project, --main-branch, "
        + "--issues, --no-issues, --wiki or --no-wiki.";

    public async Task<JsonElement> HandleAsync(UpdateRepoRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.RepoPath(credentials, request.Workspace, request.Repository,
            "Error: Invalid repository path.");

        // PUT merges here as it does on a pull request: anything left out keeps
        // its current value, so the body carries only what the caller asked to
        // change.
        var body = new Dictionary<string, object>();
        if (request.Name is not null) body["name"] = request.Name;
        if (request.Description is not null) body["description"] = request.Description;
        if (request.IsPrivate is not null) body["is_private"] = request.IsPrivate.Value;
        if (request.ForkPolicy is not null) body["fork_policy"] = request.ForkPolicy;
        if (request.Language is not null) body["language"] = request.Language;
        if (request.Website is not null) body["website"] = request.Website;
        if (request.Project is not null) body["project"] = new { key = request.Project };
        if (request.MainBranch is not null) body["mainbranch"] = new { name = request.MainBranch };
        if (request.HasIssues is not null) body["has_issues"] = request.HasIssues.Value;
        if (request.HasWiki is not null) body["has_wiki"] = request.HasWiki.Value;

        if (body.Count == 0) throw new BbxUserException(NothingToUpdate);

        return await client.PutAsync<JsonElement>($"repositories/{ws}/{repo}", body, ct);
    }
}
