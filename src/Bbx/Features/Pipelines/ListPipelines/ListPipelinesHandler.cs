using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListPipelines;

public sealed class ListPipelinesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListPipelinesRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required. Use --workspace and --repo or set defaults.");

        var query = PipelineFormat.BuildQuery(request.Status, request.Branch);
        var url = $"repositories/{ws}/{repository}/pipelines/?sort={request.Sort}";
        if (!string.IsNullOrEmpty(query))
            url += $"&q={Uri.EscapeDataString(query)}";

        var pipelines = new List<JsonElement>();
        await foreach (var pipeline in client.GetPaginatedAsync<JsonElement>(url, ct))
        {
            pipelines.Add(pipeline);
            if (pipelines.Count >= request.Limit) break;
        }

        return new
        {
            workspace = ws,
            repository,
            count = pipelines.Count,
            pipelines = pipelines.Select(PipelineFormat.Pipeline),
        };
    }
}
