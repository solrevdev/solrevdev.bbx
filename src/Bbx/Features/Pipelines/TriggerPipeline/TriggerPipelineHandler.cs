using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.TriggerPipeline;

public sealed class TriggerPipelineHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string CommitNeedsBranch =
        "Error: --commit needs --branch. Bitbucket does not check that the commit is on the branch, "
        + "so it would run against whichever branch was guessed.";


    public async Task<object> HandleAsync(TriggerPipelineRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        // Bitbucket accepts a commit that is not on the branch you name and runs
        // it anyway, labelled with that branch, so a guessed branch beside an
        // explicit commit is silently wrong rather than rejected.
        if (!string.IsNullOrEmpty(request.Commit) && string.IsNullOrWhiteSpace(request.Branch))
            throw new BbxUserException(CommitNeedsBranch);

        // --branch is optional. Without it a pull-request run takes the pull
        // request's own source branch and a branch run takes the repository's
        // main branch, both read from the API rather than assumed to be "main".
        var branch = !string.IsNullOrEmpty(request.PullRequestId)
            ? await DefaultBranch.ResolveFromPullRequestAsync(
                client, ws, repository, request.Branch, request.PullRequestId, ct)
            : await DefaultBranch.ResolveAsync(client, ws, repository, request.Branch, ct);

        var target = new Dictionary<string, object>
        {
            ["type"] = "pipeline_ref_target",
            ["ref_type"] = "branch",
            ["ref_name"] = branch,
        };

        if (!string.IsNullOrEmpty(request.PullRequestId))
        {
            // A pull-request run is a ref target on the source branch carrying a
            // pull-requests selector. There is no pull-request target type:
            // `pipeline_pullrequest_target` appears nowhere in Bitbucket's spec
            // and every spelling of it is answered with 400 "The request body
            // contains invalid properties" (four bodies tried live on
            // 2026-08-07). The selector pattern matches the source branch in the
            // `pull-requests:` section of bitbucket-pipelines.yml, so "**" is the
            // catch-all that section is normally keyed by.
            target["selector"] = new { type = "pull-requests", pattern = request.Pattern ?? "**" };
        }
        else
        {
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
