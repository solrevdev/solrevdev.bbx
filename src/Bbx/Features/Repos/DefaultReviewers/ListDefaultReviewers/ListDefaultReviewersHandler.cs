using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.DefaultReviewers.ListDefaultReviewers;

public sealed class ListDefaultReviewersHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListDefaultReviewersRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var reviewers = new List<object>();
        var count = 0;
        await foreach (var reviewer in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/default-reviewers", ct))
        {
            reviewers.Add(ProjectReviewer(reviewer));
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = reviewers.Count, reviewers };
    }

    internal static object ProjectReviewer(JsonElement r) => new
    {
        uuid = r.TryGetProperty("uuid", out var u) ? u.GetString() : null,
        account_id = r.TryGetProperty("account_id", out var a) ? a.GetString() : null,
        nickname = r.TryGetProperty("nickname", out var n) ? n.GetString() : null,
        display_name = r.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
        type = r.TryGetProperty("type", out var t) ? t.GetString() : null,
    };
}
