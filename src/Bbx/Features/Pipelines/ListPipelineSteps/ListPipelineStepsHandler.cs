using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListPipelineSteps;

public sealed class ListPipelineStepsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListPipelineStepsRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        var steps = new List<JsonElement>();
        await foreach (var step in client.GetPaginatedAsync<JsonElement>(
            $"repositories/{ws}/{repository}/pipelines/{request.PipelineUuid}/steps/", ct))
        {
            steps.Add(step);
        }

        return new
        {
            pipeline_uuid = request.PipelineUuid,
            count = steps.Count,
            steps = steps.Select(PipelineFormat.Step),
        };
    }
}
