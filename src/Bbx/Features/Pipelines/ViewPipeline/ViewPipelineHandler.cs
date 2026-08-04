using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ViewPipeline;

public sealed class ViewPipelineHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewPipelineRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        var pipeline = await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repository}/pipelines/{request.PipelineUuid}", ct);
        var steps = await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repository}/pipelines/{request.PipelineUuid}/steps/", ct);

        return new
        {
            pipeline = PipelineFormat.PipelineDetailed(pipeline),
            steps = steps.TryGetProperty("values", out var stepsArray)
                    && stepsArray.ValueKind == JsonValueKind.Array
                ? stepsArray.EnumerateArray().Select(PipelineFormat.Step).ToList()
                : [],
        };
    }
}
