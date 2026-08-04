using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ViewDeploymentEnvironment;

public sealed class ViewDeploymentEnvironmentHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewDeploymentEnvironmentRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        var env = await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repository}/environments/{request.Environment}", ct);

        return new
        {
            uuid = PipelineFormat.GetString(env, "uuid"),
            name = PipelineFormat.GetString(env, "name"),
            environment_type = env.TryGetObject("environment_type", out var et) ? (object)new
            {
                name = PipelineFormat.GetString(et, "name"),
                rank = et.TryGetProperty("rank", out var r) ? r.GetInt32() : 0,
            } : null!,
            deployment_gate_enabled = env.TryGetProperty("deployment_gate_enabled", out var dg) && dg.GetBoolean(),
            lock_ = env.TryGetObject("lock", out var l) ? (object)new
            {
                type = PipelineFormat.GetString(l, "type"),
                name = PipelineFormat.GetString(l, "name"),
            } : null!,
            restrictions = env.TryGetProperty("restrictions", out var res) ? (object)res : null!,
        };
    }
}
