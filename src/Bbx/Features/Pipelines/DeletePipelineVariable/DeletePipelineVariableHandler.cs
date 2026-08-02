using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.DeletePipelineVariable;

public sealed class DeletePipelineVariableHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(DeletePipelineVariableRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        await client.DeleteAsync(
            $"repositories/{ws}/{repository}/pipelines_config/variables/{request.Uuid}", ct);

        return new { message = "Variable deleted successfully", uuid = request.Uuid };
    }
}
