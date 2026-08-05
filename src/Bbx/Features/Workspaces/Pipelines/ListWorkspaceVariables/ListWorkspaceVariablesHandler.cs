using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.Pipelines;

namespace Bbx.Features.Workspaces.Pipelines.ListWorkspaceVariables;

public sealed class ListWorkspaceVariablesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListWorkspaceVariablesRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
        var endpoint = $"workspaces/{workspace}/pipelines-config/variables";

        var variables = new List<object>();
        var count = 0;
        await foreach (var variable in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            variables.Add(PipelineFormat.Variable(variable));
            if (++count >= request.Limit) break;
        }

        return new { workspace, count = variables.Count, variables };
    }
}
