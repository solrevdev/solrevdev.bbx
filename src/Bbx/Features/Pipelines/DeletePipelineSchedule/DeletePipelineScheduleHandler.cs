using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.DeletePipelineSchedule;

public sealed class DeletePipelineScheduleHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(DeletePipelineScheduleRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        await client.DeleteAsync(
            $"repositories/{ws}/{repository}/pipelines_config/schedules/{request.Uuid}", ct);

        return new { message = "Schedule deleted successfully", uuid = request.Uuid };
    }
}
