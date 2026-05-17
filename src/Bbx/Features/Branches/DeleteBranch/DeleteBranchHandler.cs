using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Branches.DeleteBranch;

public sealed class DeleteBranchHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteBranchRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.DeleteAsync(
            $"/repositories/{ws}/{repo}/refs/branches/{Uri.EscapeDataString(request.Name)}", ct);
        return $"✓ Deleted branch '{request.Name}'";
    }
}
