using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListPipelineKnownHosts;

public sealed class ListPipelineKnownHostsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListPipelineKnownHostsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        var hosts = new List<object>();
        var count = 0;
        await foreach (var host in client.GetPaginatedAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pipelines_config/ssh/known_hosts", ct))
        {
            hosts.Add(PipelineFormat.KnownHost(host));
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = hosts.Count, known_hosts = hosts };
    }
}
