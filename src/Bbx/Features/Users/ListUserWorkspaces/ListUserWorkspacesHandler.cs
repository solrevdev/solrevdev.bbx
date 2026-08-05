using System.Text.Json;
using Bbx.Api;

namespace Bbx.Features.Users.ListUserWorkspaces;

public sealed class ListUserWorkspacesHandler(BitbucketClient client)
{
    public async Task<object> HandleAsync(ListUserWorkspacesRequest request, CancellationToken ct)
    {
        // The account-scoped list. /2.0/workspaces, which `workspace list`
        // calls, is 410 Gone.
        var workspaces = new List<object>();
        var count = 0;
        await foreach (var w in client.GetPaginatedAsync<JsonElement>("user/workspaces", ct))
        {
            workspaces.Add(new
            {
                slug = w.GetStringOrNull("slug"),
                name = w.GetStringOrNull("name"),
                uuid = w.GetStringOrNull("uuid"),
                type = w.GetStringOrNull("type"),
            });
            if (++count >= request.Limit) break;
        }

        return new { count = workspaces.Count, workspaces };
    }
}
