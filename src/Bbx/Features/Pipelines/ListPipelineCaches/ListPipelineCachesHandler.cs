using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListPipelineCaches;

public sealed class ListPipelineCachesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListPipelineCachesRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        var caches = new List<JsonElement>();
        await foreach (var c in client.GetPaginatedAsync<JsonElement>(
            $"repositories/{ws}/{repository}/pipelines-config/caches/", ct))
        {
            caches.Add(c);
        }

        return new
        {
            workspace = ws,
            repository,
            count = caches.Count,
            caches = caches.Select(c => new
            {
                uuid = PipelineFormat.GetString(c, "uuid"),
                name = PipelineFormat.GetString(c, "name"),
                created_on = PipelineFormat.GetString(c, "created_on"),
                file_size_bytes = c.TryGetProperty("file_size_bytes", out var fs) ? fs.GetInt64() : 0,
            }),
        };
    }
}
