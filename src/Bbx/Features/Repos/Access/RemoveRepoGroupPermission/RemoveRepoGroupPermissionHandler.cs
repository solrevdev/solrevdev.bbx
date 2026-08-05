using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.Access.RemoveRepoGroupPermission;

public sealed class RemoveRepoGroupPermissionHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(RemoveRepoGroupPermissionRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.DeleteAsync(
            $"repositories/{ws}/{repo}/permissions-config/groups/{Uri.EscapeDataString(request.GroupSlug)}", ct);
        return $"\u2713 Removed the explicit permission for {request.GroupSlug} on {ws}/{repo}";
    }
}
