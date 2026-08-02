using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Branches.ListBranchRestrictions;

public sealed class ListBranchRestrictionsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListBranchRestrictionsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var restrictions = new List<object>();
        await foreach (var restriction in client.GetPaginatedAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/branch-restrictions", ct))
        {
            restrictions.Add(new
            {
                id = restriction.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                kind = restriction.TryGetProperty("kind", out var k) ? k.GetString() : null,
                pattern = restriction.TryGetProperty("pattern", out var p) ? p.GetString() : null,
                branch_match_kind = restriction.TryGetProperty("branch_match_kind", out var bmk) ? bmk.GetString() : null,
            });
        }

        return new { count = restrictions.Count, restrictions };
    }
}
