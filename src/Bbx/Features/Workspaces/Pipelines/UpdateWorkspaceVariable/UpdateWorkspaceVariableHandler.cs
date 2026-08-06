using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.Pipelines;

namespace Bbx.Features.Workspaces.Pipelines.UpdateWorkspaceVariable;

public sealed class UpdateWorkspaceVariableHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string NothingToUpdate =
        "Error: nothing to update. Pass --key, --value, --secured or --unsecured.";

    public async Task<object> HandleAsync(UpdateWorkspaceVariableRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
        var endpoint = $"workspaces/{workspace}/pipelines-config/variables";

        var body = new Dictionary<string, object> { ["type"] = "pipeline_variable" };
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
