using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.TriggerPipeline;

public sealed class TriggerPipelineHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(TriggerPipelineRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        Dictionary<string, object> target;
        if (!string.IsNullOrEmpty(request.PullRequestId))
        {
            // pipeline_pullrequest_target — runs the pull-request pipeline for
            // the named PR. `source` is the source branch of the PR; Bitbucket
            // fills in destination/destination_commit from the PR itself.
            target = new Dictionary<string, object>
            {
                ["type"] = "pipeline_pullrequest_target",
                ["source"] = request.Branch,
                ["pullrequest"] = new { id = request.PullRequestId },
            };
            if (!string.IsNullOrEmpty(request.Pattern))
                target["selector"] = new { type = "pull-requests", pattern = request.Pattern };
        }
        else
        {
            target = new Dictionary<string, object>
            {
                ["type"] = "pipeline_ref_target",
                ["ref_type"] = "branch",
                ["ref_name"] = request.Branch,
            };
            if (!string.IsNullOrEmpty(request.Commit))
                target["commit"] = new { hash = request.Commit };
            if (!string.IsNullOrEmpty(request.Pattern))
                target["selector"] = new { type = "custom", pattern = request.Pattern };
        }

        var body = new Dictionary<string, object> { ["target"] = target };

        if (request.Variables.Length > 0)
        {
            var pipelineVars = request.Variables
                .Select(v => v.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .Select(parts => new
                {
                    key = parts[0],
                    value = parts[1],
                    secured = parts[0].Contains("SECRET", StringComparison.OrdinalIgnoreCase),
                })
                .ToList();
            body["variables"] = pipelineVars;
        }

        var pipeline = await client.PostAsync<JsonElement>($"repositories/{ws}/{repository}/pipelines/", body, ct);

        return new
        {
            message = "Pipeline triggered successfully",
            pipeline = PipelineFormat.Pipeline(pipeline),
        };
    }
}
