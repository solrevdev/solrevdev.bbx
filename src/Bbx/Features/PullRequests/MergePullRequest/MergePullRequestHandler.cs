using System.Text.Json;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.PullRequests.PullRequestMergeStatus;

namespace Bbx.Features.PullRequests.MergePullRequest;

public sealed class MergePullRequestHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<CommandOutcome> HandleAsync(MergePullRequestRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var strategy = await ResolveStrategyAsync(ws, repo, request, ct);

        var body = new Dictionary<string, object>
        {
            ["type"] = "pullrequest",
            ["merge_strategy"] = strategy,
            ["close_source_branch"] = request.CloseSourceBranch,
        };
        if (!string.IsNullOrEmpty(request.Message)) body["message"] = request.Message;

        var response = await client.PostForResponseAsync(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/merge", body, ct);

        // 200 carries the merged pull request. 202 means Bitbucket queued the
        // merge and named the task only in the Location header; this used to
        // print the empty body as "" and exit 0, leaving the PR open and the
        // task ID nowhere (16 Nov 2026, during a Bitbucket slowdown).
        if (response.Status != 202) return new CommandOutcome(response.Body);

        var statusUrl = MergeTask.StatusUrl(response);
        var taskId = statusUrl is null ? null : MergeTask.IdFrom(statusUrl);
        if (taskId is null)
        {
            return new CommandOutcome(
                MergeTask.Queued(ws, repo, request.Id, null, null, null, null),
                ExitCodes.Pending,
                $"Bitbucket queued the merge of PR #{request.Id} but sent no task link. "
                + "Check the pull request's state before merging again.");
        }

        if (!request.Wait)
            return new CommandOutcome(MergeTask.Queued(ws, repo, request.Id, taskId, statusUrl, null, null));

        return await MergeTask.WaitAsync(client, ws, repo, request.Id, taskId, statusUrl,
            request.TimeoutSeconds, request.PollInterval ?? MergeTask.DefaultInterval, ct);
    }

    /// <summary>
    /// Work out which merge strategy to send, asking the destination branch
    /// rather than assuming.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A repository can change its default strategy, and <c>--strategy</c> used
    /// to default to merge_commit whatever it said. The value lives on the
    /// branch: <c>GET refs/branches/{name}</c> returns
    /// <c>default_merge_strategy</c>. The pull request does not carry it,
    /// whatever the spec says; <c>destination.branch</c> comes back as
    /// <c>{"name": "master"}</c> and nothing else.
    /// </para>
    /// <para>
    /// The same call returns <c>merge_strategies</c>, and this used to refuse a
    /// strategy missing from that list. It no longer does. Nothing can shrink
    /// the list: the repository's Merge strategies page offers a default and no
    /// per-strategy control, and a <c>restrict_merges</c> branch restriction
    /// leaves it alone, so it always names all six. A check that cannot fire can
    /// only ever be wrong, and the one thing it could do is block a merge that
    /// would have worked.
    /// </para>
    /// </remarks>
    private async Task<string> ResolveStrategyAsync(
        string ws, string repo, MergePullRequestRequest request, CancellationToken ct)
    {
        var asked = NormalizeStrategy(request.Strategy);

        // Both lookups are advisory. The merge is what was asked for, so a
        // failure to read the branch must not stop it: fall back to what the
        // caller wanted and let Bitbucket have the last word, which is what
        // happened before these two calls existed.
        // An explicit strategy is the caller's business, so do not spend two
        // calls confirming it.
        if (asked is not null) return asked;

        try
        {
            var destination = await DestinationBranchAsync(ws, repo, request.Id, ct);
            if (destination is null) return DefaultStrategy;

            var branch = await client.GetAsync<JsonElement>(
                $"repositories/{ws}/{repo}/refs/branches/{Uri.EscapeDataString(destination)}", ct);

            return branch.GetStringOrNull("default_merge_strategy") ?? DefaultStrategy;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return DefaultStrategy;
        }
    }

    private async Task<string?> DestinationBranchAsync(string ws, string repo, int id, CancellationToken ct)
    {
        var pr = await client.GetAsync<JsonElement>($"repositories/{ws}/{repo}/pullrequests/{id}", ct);
        return pr.TryGetObject("destination", out var destination)
               && destination.TryGetObject("branch", out var branch)
            ? branch.GetStringOrNull("name")
            : null;
    }

    private const string DefaultStrategy = "merge_commit";

    /// <summary>
    /// Bitbucket accepts merge_commit, squash, fast_forward, squash_fast_forward,
    /// rebase_fast_forward and rebase_merge. "merge" is what the UI calls a merge
    /// commit and was this command's default, so it was rejected with
    /// "merge_strategy: Select a valid choice". Anything unrecognised is passed
    /// through, because the list is Bitbucket's to grow.
    /// </summary>
    internal static string? NormalizeStrategy(string? strategy) => strategy?.Trim().ToLowerInvariant() switch
    {
        null or "" => null,
        "merge" or "merge_commit" => "merge_commit",
        "squash" => "squash",
        "fast_forward" or "fast-forward" or "ff" => "fast_forward",
        "squash_fast_forward" or "squash-fast-forward" => "squash_fast_forward",
        "rebase_fast_forward" or "rebase-fast-forward" => "rebase_fast_forward",
        "rebase_merge" or "rebase-merge" => "rebase_merge",
        var other => other,
    };
}
