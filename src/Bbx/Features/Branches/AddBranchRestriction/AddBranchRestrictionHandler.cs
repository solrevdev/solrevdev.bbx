using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Branches.AddBranchRestriction;

public sealed class AddBranchRestrictionHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(AddBranchRestrictionRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        // Always glob. Bitbucket rejects a pattern sent with any other match kind
        // ("pattern is only valid when branch_match_kind is glob"), and
        // branching_model expects a branch_type instead, which this command does
        // not take. An exact branch name is a valid glob, so --pattern master
        // works as the option's help promises.
        var body = new
        {
            kind = request.Kind,
            pattern = request.Pattern,
            branch_match_kind = "glob",
        };
        return await client.PostAsync<JsonElement>($"/repositories/{ws}/{repo}/branch-restrictions", body, ct);
    }
}
