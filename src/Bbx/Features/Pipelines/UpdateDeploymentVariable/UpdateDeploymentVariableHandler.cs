using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.UpdateDeploymentVariable;

public sealed class UpdateDeploymentVariableHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string NothingToUpdate =
        "Error: nothing to update. Pass --key, --value, --secured or --unsecured.";

    public async Task<object> HandleAsync(UpdateDeploymentVariableRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        var endpoint = $"repositories/{ws}/{repo}/deployments_config/environments"
            + $"/{Uri.EscapeDataString(request.Environment)}/variables";

        var body = new Dictionary<string, object> { ["type"] = "deployment_variable" };
        if (request.Key is not null) body["key"] = request.Key;
        if (request.Value is not null) body["value"] = request.Value;
        if (request.Secured is not null) body["secured"] = request.Secured.Value;
        // The type discriminator is always sent, so the count is against one.
        if (body.Count == 1) throw new BbxUserException(NothingToUpdate);

        var variable = await client.PutAsync<JsonElement>(
            $"{endpoint}/{Uri.EscapeDataString(request.VariableUuid)}", body, ct);
        return PipelineFormat.Variable(variable);
    }
}
