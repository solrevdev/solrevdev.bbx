using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.Pipelines;

namespace Bbx.Features.Workspaces.Pipelines.AddWorkspaceVariable;

public sealed class AddWorkspaceVariableHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(AddWorkspaceVariableRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
        var endpoint = $"workspaces/{workspace}/pipelines-config/variables";

        var variable = await client.PostAsync<JsonElement>(endpoint, new
        {
            type = "pipeline_variable",
            key = request.Key,
            value = request.Value,
            secured = request.Secured,
        }, ct);
        return PipelineFormat.Variable(variable);
    }
}
