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
        var body = new
        {
            kind = request.Kind,
            pattern = request.Pattern,
            branch_match_kind = request.Pattern.Contains('*') ? "glob" : "branching_model",
        };
        return await client.PostAsync<JsonElement>($"/repositories/{ws}/{repo}/branch-restrictions", body, ct);
    }
}
