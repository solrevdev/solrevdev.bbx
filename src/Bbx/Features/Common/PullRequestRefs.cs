using System.Text.Json;

namespace Bbx.Features.Common;

/// <summary>
/// The four refs a pull-request pipeline target has to carry, read off the pull
/// request itself.
/// </summary>
internal sealed record PullRequestRefs(
    string Source,
    string SourceCommit,
    string Destination,
    string DestinationCommit)
{
    /// <summary>
    /// Read them from <c>GET pullrequests/{id}</c>.
    /// </summary>
    /// <remarks>
    /// All four are required by the pipelines endpoint. Dropping
    /// <c>destination</c>, <c>destination_commit</c>, <c>source</c> or
    /// <c>pullrequest</c> is answered 400 "The request body contains invalid
    /// properties"; dropping <c>commit</c> is answered 500. Only
    /// <c>selector</c> is optional. Established by posting the shape Bitbucket
    /// itself produced for a pull-request run and removing one field at a time,
    /// live on 2026-08-07.
    /// </remarks>
    public static async Task<PullRequestRefs> ReadAsync(
        BitbucketClient client, string workspace, string repository, string pullRequestId, CancellationToken ct)
    {
        var pr = await client.GetAsync<JsonElement>(
            $"repositories/{workspace}/{repository}/pullrequests/{pullRequestId}", ct);

        var source = Endpoint(pr, "source");
        var destination = Endpoint(pr, "destination");

        if (source is null || destination is null)
            throw new BbxUserException(
                $"Error: pull request {pullRequestId} does not report both of its branches, "
                + "so the pipeline cannot be targeted at it.");

        return new PullRequestRefs(source.Value.Branch, source.Value.Commit,
            destination.Value.Branch, destination.Value.Commit);
    }

    private static (string Branch, string Commit)? Endpoint(JsonElement pr, string name)
    {
        if (!pr.TryGetObject(name, out var endpoint)) return null;

        var branch = endpoint.TryGetObject("branch", out var b) ? b.GetStringOrNull("name") : null;
        var commit = endpoint.TryGetObject("commit", out var c) ? c.GetStringOrNull("hash") : null;

        return branch is null || commit is null ? null : (branch, commit);
    }
}
