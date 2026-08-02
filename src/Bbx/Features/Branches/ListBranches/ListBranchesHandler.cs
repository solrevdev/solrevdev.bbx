using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Branches.ListBranches;

public sealed class ListBranchesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListBranchesRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var endpoint = $"/repositories/{ws}/{repo}/refs/branches";
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(request.Sort)) queryParams.Add($"sort={request.Sort}");
        if (!string.IsNullOrEmpty(request.Query)) queryParams.Add($"q={Uri.EscapeDataString(request.Query)}");
        if (queryParams.Count > 0) endpoint += "?" + string.Join("&", queryParams);

        var branches = new List<object>();
        var count = 0;
        await foreach (var branch in client.GetPaginatedAsync<JsonElement>(endpoint, ct))
        {
            branches.Add(new
            {
                name = branch.TryGetProperty("name", out var n) ? n.GetString() : null,
                target = branch.TryGetObject("target", out var t) && t.TryGetProperty("hash", out var h) ? h.GetString()?[..12] : null,
                author = branch.TryGetObject("target", out var t2) && t2.TryGetObject("author", out var a) && a.TryGetObject("user", out var u) && u.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
                date = branch.TryGetObject("target", out var t3) && t3.TryGetProperty("date", out var d) ? d.GetString() : null,
            });
            if (++count >= request.Limit) break;
        }

        return new { workspace = ws, repository = repo, count = branches.Count, branches };
    }
}
