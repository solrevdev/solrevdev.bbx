using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.Access.RemoveRepoUserPermission;

public sealed class RemoveRepoUserPermissionHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(RemoveRepoUserPermissionRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.DeleteAsync(
            $"repositories/{ws}/{repo}/permissions-config/users/{Uri.EscapeDataString(request.AccountId)}", ct);
        return $"\u2713 Removed the explicit permission for {request.AccountId} on {ws}/{repo}";
    }
}
