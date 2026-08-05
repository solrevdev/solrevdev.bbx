using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListDeployments;

public sealed class ListDeploymentsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListDeploymentsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        // Deployments are the records of what was released where. The
        // environments they target are managed under `pipeline deployments`.
        var deployments = new List<object>();
        var count = 0;
        await foreach (var deployment in client.GetPaginatedAsync<JsonElement>(
            $"repositories/{ws}/{repo}/deployments", ct))
        {
            deployments.Add(Summary(deployment));
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = deployments.Count, deployments };
    }

    internal static object Summary(JsonElement deployment) => new
    {
        uuid = PipelineFormat.GetString(deployment, "uuid"),
        environment = deployment.TryGetObject("environment", out var e)
            ? PipelineFormat.GetString(e, "name") : null,
        state = deployment.TryGetObject("state", out var s) ? PipelineFormat.GetString(s, "name") : null,
        // The release carries the commit and the pipeline that produced it.
        commit = deployment.TryGetObject("release", out var release)
                 && release.TryGetObject("commit", out var commit)
            ? PipelineFormat.GetString(commit, "hash") : null,
        created_on = deployment.TryGetObject("release", out var r)
            ? PipelineFormat.GetString(r, "created_on") : null,
    };
}
