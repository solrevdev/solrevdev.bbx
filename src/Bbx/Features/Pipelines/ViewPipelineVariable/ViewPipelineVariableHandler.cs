using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ViewPipelineVariable;

public sealed class ViewPipelineVariableHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewPipelineVariableRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        var variable = await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pipelines_config/variables/{Uri.EscapeDataString(request.VariableUuid)}", ct);
        return PipelineFormat.Variable(variable);
    }
}
