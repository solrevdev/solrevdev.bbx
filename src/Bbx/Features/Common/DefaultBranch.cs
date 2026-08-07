using System.Text.Json;

namespace Bbx.Features.Common;

/// <summary>
/// Works out which branch a command meant when <c>--branch</c> was left off.
/// </summary>
/// <remarks>
/// <para>
/// <c>pipeline trigger</c> and <c>pipeline schedules create</c> both defaulted
/// <c>--branch</c> to "main". A repository whose default branch is called
/// something else answered 404, which is survivable. The bad case is a
/// repository that still has a stale "main" beside a live "master": the run
/// succeeded against the wrong branch and reported success, and a schedule
/// repeated that on a cron.
/// </para>
/// <para>
/// Ask the API rather than guess. The repository object carries
/// <c>mainbranch.name</c>, which is the same thing <c>src ls</c> relies on when
/// it lists the root of the default branch without being told its name.
/// </para>
/// </remarks>
internal static class DefaultBranch
{
    public static async Task<string> ResolveAsync(
        BitbucketClient client,
        string workspace,
        string repository,
        string? branch,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(branch)) return branch;

        var repo = await client.GetAsync<JsonElement>($"repositories/{workspace}/{repository}", ct);
        var name = repo.TryGetObject("mainbranch", out var mainbranch)
            ? mainbranch.GetStringOrNull("name")
            : null;

        if (string.IsNullOrEmpty(name))
            throw new BbxUserException(
                $"Error: --branch is required. {workspace}/{repository} reports no main branch.");

        return name;
    }

    /// <summary>
    /// A pull-request pipeline runs on the pull request's source branch, so the
    /// repository's default branch is the wrong answer here. Read the source
    /// branch off the pull request instead.
    /// </summary>
    public static async Task<string> ResolveFromPullRequestAsync(
        BitbucketClient client,
        string workspace,
        string repository,
        string? branch,
        string pullRequestId,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(branch)) return branch;

        var pr = await client.GetAsync<JsonElement>(
            $"repositories/{workspace}/{repository}/pullrequests/{pullRequestId}", ct);
        var name = pr.TryGetObject("source", out var source) && source.TryGetObject("branch", out var sourceBranch)
            ? sourceBranch.GetStringOrNull("name")
            : null;

        if (string.IsNullOrEmpty(name))
            throw new BbxUserException(
                $"Error: --branch is required. Pull request {pullRequestId} reports no source branch.");

        return name;
    }
}
