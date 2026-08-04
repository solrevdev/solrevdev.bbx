using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ListDeploymentEnvironments;

public sealed class ListDeploymentEnvironmentsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListDeploymentEnvironmentsRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        var environments = new List<JsonElement>();
        await foreach (var env in client.GetPaginatedAsync<JsonElement>(
            $"repositories/{ws}/{repository}/environments/", ct))
        {
            environments.Add(env);
        }

        return new
        {
            workspace = ws,
            repository,
            count = environments.Count,
            environments = environments.Select(e => new
            {
                uuid = PipelineFormat.GetString(e, "uuid"),
                name = PipelineFormat.GetString(e, "name"),
                environment_type = e.TryGetObject("environment_type", out var et)
                    ? PipelineFormat.GetString(et, "name")
                    : null,
                rank = e.TryGetProperty("rank", out var r) ? r.GetInt32() : 0,
                deployment_gate_enabled = e.TryGetProperty("deployment_gate_enabled", out var dg) && dg.GetBoolean(),
            }),
        };
    }
}
