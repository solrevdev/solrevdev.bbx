using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.MergePullRequest;

public sealed class MergePullRequestHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(MergePullRequestRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var body = new Dictionary<string, object>
        {
            ["type"] = "pullrequest",
            ["merge_strategy"] = NormalizeStrategy(request.Strategy),
            ["close_source_branch"] = request.CloseSourceBranch,
        };
        if (!string.IsNullOrEmpty(request.Message)) body["message"] = request.Message;

        return await client.PostAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/pullrequests/{request.Id}/merge", body, ct);
    }

    /// <summary>
    /// Bitbucket accepts merge_commit, squash and fast_forward. "merge" is what
    /// the UI calls a merge commit and was this command's default, so it was
    /// rejected with "merge_strategy: Select a valid choice".
    /// </summary>
    internal static string NormalizeStrategy(string? strategy) => strategy?.Trim().ToLowerInvariant() switch
    {
        null or "" or "merge" or "merge_commit" => "merge_commit",
        "squash" => "squash",
        "fast_forward" or "fast-forward" or "ff" => "fast_forward",
        var other => other,
    };
}
