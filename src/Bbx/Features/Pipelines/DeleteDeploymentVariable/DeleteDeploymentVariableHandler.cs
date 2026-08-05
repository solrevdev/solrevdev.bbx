using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.DeleteDeploymentVariable;

public sealed class DeleteDeploymentVariableHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteDeploymentVariableRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        var endpoint = $"repositories/{ws}/{repo}/deployments_config/environments"
            + $"/{Uri.EscapeDataString(request.Environment)}/variables";
        await client.DeleteAsync($"{endpoint}/{Uri.EscapeDataString(request.VariableUuid)}", ct);
        return $"\u2713 Deleted deployment variable {request.VariableUuid}";
    }
}
