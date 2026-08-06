using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.DeleteDeploymentEnvironment;

public sealed class DeleteDeploymentEnvironmentHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteDeploymentEnvironmentRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        await client.DeleteAsync(
            $"repositories/{ws}/{repo}/environments/{Uri.EscapeDataString(request.Environment)}", ct);
        return $"\u2713 Deleted deployment environment {request.Environment}";
    }
}
