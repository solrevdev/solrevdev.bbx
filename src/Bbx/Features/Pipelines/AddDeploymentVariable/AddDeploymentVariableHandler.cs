using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.AddDeploymentVariable;

public sealed class AddDeploymentVariableHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(AddDeploymentVariableRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        var endpoint = $"repositories/{ws}/{repo}/deployments_config/environments"
            + $"/{Uri.EscapeDataString(request.Environment)}/variables";

        var variable = await client.PostAsync<JsonElement>(endpoint, new
        {
            type = "deployment_variable",
            key = request.Key,
            value = request.Value,
            secured = request.Secured,
        }, ct);
        return PipelineFormat.Variable(variable);
    }
}
