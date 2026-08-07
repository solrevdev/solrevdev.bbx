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

    public const string BranchAndPullRequest =
        "Error: --branch cannot be combined with --pull-request. Both branches and both commits "
        + "come from the pull request; a run against any other branch is not a pull-request run.";

    public async Task<object> HandleAsync(TriggerPipelineRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        // Bitbucket accepts a commit that is not on the branch you name and runs
        // it anyway, labelled with that branch, so a guessed branch beside an
        // explicit commit is silently wrong rather than rejected.
        if (!string.IsNullOrEmpty(request.Commit) && string.IsNullOrWhiteSpace(request.Branch))
            throw new BbxUserException(CommitNeedsBranch);

        var target = !string.IsNullOrEmpty(request.PullRequestId)
            ? await PullRequestTargetAsync(ws, repository, request, ct)
            : await BranchTargetAsync(ws, repository, request, ct);

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

    /// <summary>
    /// Build the target for a pull-request run.
    /// </summary>
    /// <remarks>
    /// This is a real <c>pipeline_pullrequest_target</c>, and it has to carry
    /// both branches and both commits. A <c>pipeline_ref_target</c> on the
    /// source branch with a <c>pull-requests</c> selector does run the steps
    /// under <c>pull-requests:</c>, which is why it looked right, but the run is
    /// not a pull-request run: <c>BITBUCKET_PR_ID</c> and
    /// <c>BITBUCKET_PR_DESTINATION_BRANCH</c> are absent from it entirely, so a
    /// script reading either gets an empty string. Both were read out of a live
    /// build's log on 2026-08-07, empty for the ref target and populated here.
    /// The id may be a string or a number. <c>selector</c> is the only optional
    /// field.
    /// </remarks>
    private async Task<Dictionary<string, object>> PullRequestTargetAsync(
        string ws, string repository, TriggerPipelineRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Branch))
            throw new BbxUserException(BranchAndPullRequest);

        var refs = await PullRequestRefs.ReadAsync(client, ws, repository, request.PullRequestId!, ct);

        var target = new Dictionary<string, object>
        {
            ["type"] = "pipeline_pullrequest_target",
            ["source"] = refs.Source,
            ["destination"] = refs.Destination,
            ["commit"] = new { hash = refs.SourceCommit },
            ["destination_commit"] = new { hash = refs.DestinationCommit },
            ["pullrequest"] = new { id = request.PullRequestId },
        };

        if (!string.IsNullOrEmpty(request.Pattern))
            target["selector"] = new { type = "pull-requests", pattern = request.Pattern };

        return target;
    }

    private async Task<Dictionary<string, object>> BranchTargetAsync(
        string ws, string repository, TriggerPipelineRequest request, CancellationToken ct)
    {
        // Without --branch, take the repository's own main branch rather than
        // assume "main".
        var branch = await DefaultBranch.ResolveAsync(client, ws, repository, request.Branch, ct);

        var target = new Dictionary<string, object>
        {
            ["type"] = "pipeline_ref_target",
            ["ref_type"] = "branch",
            ["ref_name"] = branch,
        };

        if (!string.IsNullOrEmpty(request.Commit))
            target["commit"] = new { hash = request.Commit };
        if (!string.IsNullOrEmpty(request.Pattern))
            target["selector"] = new { type = "custom", pattern = request.Pattern };

        return target;
    }
}
