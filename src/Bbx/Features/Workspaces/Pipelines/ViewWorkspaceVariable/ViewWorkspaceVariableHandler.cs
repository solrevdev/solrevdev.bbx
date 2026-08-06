using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.Pipelines;

namespace Bbx.Features.Workspaces.Pipelines.ViewWorkspaceVariable;

public sealed class ViewWorkspaceVariableHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewWorkspaceVariableRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
        var endpoint = $"workspaces/{workspace}/pipelines-config/variables";
        var variable = await client.GetAsync<JsonElement>(
            $"{endpoint}/{Uri.EscapeDataString(request.VariableUuid)}", ct);
        return PipelineFormat.Variable(variable);
    }
}
