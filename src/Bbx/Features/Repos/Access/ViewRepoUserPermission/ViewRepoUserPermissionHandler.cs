using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.Access.ViewRepoUserPermission;

public sealed class ViewRepoUserPermissionHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewRepoUserPermissionRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        var grant = await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/permissions-config/users/{Uri.EscapeDataString(request.AccountId)}", ct);
        return PermissionGrant.Project(grant);
    }
}
