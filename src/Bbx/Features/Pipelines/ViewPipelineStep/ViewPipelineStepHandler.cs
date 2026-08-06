using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ViewPipelineStep;

public sealed class ViewPipelineStepHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewPipelineStepRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        var step = await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pipelines/{Uri.EscapeDataString(request.PipelineUuid)}/steps/{Uri.EscapeDataString(request.StepUuid)}", ct);
        return PipelineFormat.Step(step);
    }
}
