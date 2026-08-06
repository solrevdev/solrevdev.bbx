using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ViewDeployment;

public sealed class ViewDeploymentHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewDeploymentRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        return await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/deployments/{Uri.EscapeDataString(request.DeploymentUuid)}", ct);
    }
}
