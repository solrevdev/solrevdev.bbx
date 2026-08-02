using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Branches.DeleteBranchRestriction;

public sealed class DeleteBranchRestrictionHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteBranchRestrictionRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.DeleteAsync($"/repositories/{ws}/{repo}/branch-restrictions/{request.Id}", ct);
        return $"✓ Deleted branch restriction #{request.Id}";
    }
}
