using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.AddPipelineVariable;

public sealed class AddPipelineVariableHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(AddPipelineVariableRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        var body = new
        {
            type = "pipeline_variable",
            key = request.Key,
            value = request.Value,
            secured = request.Secured,
        };

        var variable = await client.PostAsync<JsonElement>(
            $"repositories/{ws}/{repository}/pipelines_config/variables/", body, ct);

        return new
        {
            message = "Variable added successfully",
            variable = new
            {
                uuid = PipelineFormat.GetString(variable, "uuid"),
                key = PipelineFormat.GetString(variable, "key"),
                secured = request.Secured,
            },
        };
    }
}
