using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListDeploymentVariables;

public sealed class ListDeploymentVariablesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListDeploymentVariablesRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        var endpoint = $"repositories/{ws}/{repo}/deployments_config/environments"
            + $"/{Uri.EscapeDataString(request.Environment)}/variables";

        var variables = new List<object>();
        var count = 0;
        await foreach (var variable in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            variables.Add(PipelineFormat.Variable(variable));
            if (++count >= request.Limit) break;
        }

        return new { environment = request.Environment, count = variables.Count, variables };
    }
}
