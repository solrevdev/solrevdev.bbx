using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListPipelineVariables;

public sealed class ListPipelineVariablesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListPipelineVariablesRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        var variables = new List<JsonElement>();
        await foreach (var v in client.GetPaginatedAsync<JsonElement>(
            $"repositories/{ws}/{repository}/pipelines_config/variables/", ct))
        {
            variables.Add(v);
        }

        return new
        {
            workspace = ws,
            repository,
            count = variables.Count,
            variables = variables.Select(v => new
            {
                uuid = PipelineFormat.GetString(v, "uuid"),
                key = PipelineFormat.GetString(v, "key"),
                secured = IsTrue(v, "secured"),
                value = IsTrue(v, "secured") ? "***" : PipelineFormat.GetString(v, "value"),
            }),
        };
    }

    private static bool IsTrue(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value)
        && value.ValueKind is JsonValueKind.True;
}
