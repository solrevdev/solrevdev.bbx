using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.Access.ViewRepoGroupPermission;

public sealed class ViewRepoGroupPermissionHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewRepoGroupPermissionRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        var grant = await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/permissions-config/groups/{Uri.EscapeDataString(request.GroupSlug)}", ct);
        return PermissionGrant.Project(grant);
    }
}
