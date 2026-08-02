using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.DefaultReviewers.EffectiveDefaultReviewers;

public sealed class EffectiveDefaultReviewersHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(EffectiveDefaultReviewersRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var reviewers = new List<object>();
        var count = 0;
        await foreach (var reviewer in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/effective-default-reviewers", ct))
        {
            reviewers.Add(new
            {
                uuid = reviewer.TryGetProperty("uuid", out var u) ? u.GetString() : null,
                account_id = reviewer.TryGetProperty("account_id", out var a) ? a.GetString() : null,
                nickname = reviewer.TryGetProperty("nickname", out var n) ? n.GetString() : null,
                display_name = reviewer.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
                type = reviewer.TryGetProperty("type", out var t) ? t.GetString() : null,
                reviewer_type = reviewer.TryGetProperty("reviewer_type", out var rt) ? rt.GetString() : null,
            });
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = reviewers.Count, reviewers };
    }
}
