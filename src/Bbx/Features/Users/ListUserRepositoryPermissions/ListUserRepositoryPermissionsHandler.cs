using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Features.Users.ListUserRepositoryPermissions;

public sealed class ListUserRepositoryPermissionsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListUserRepositoryPermissionsRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");

        var perms = new List<object>();
        var count = 0;
        await foreach (var p in client.GetPaginatedAsync<JsonElement>("/user/permissions/repositories", ct))
        {
            perms.Add(new
            {
                permission = p.TryGetProperty("permission", out var perm) ? perm.GetString() : null,
                repository_full_name = p.TryGetProperty("repository", out var r) && r.TryGetProperty("full_name", out var fn)
                    ? fn.GetString()
                    : null,
                added_on = p.TryGetProperty("added_on", out var ao) ? ao.GetString() : null,
            });
            if (++count >= request.Limit) break;
        }

        return new { count = perms.Count, repository_permissions = perms };
    }
}
