using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.ListWorkspaceMembers;

public sealed class ListWorkspaceMembersHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListWorkspaceMembersRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");

        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        var members = new List<object>();
        await foreach (var member in client.GetPaginatedAsync<JsonElement>($"workspaces/{workspace}/members", ct))
        {
            var user = member.TryGetProperty("user", out var u) ? u : member;

            members.Add(new
            {
                display_name = user.TryGetProperty("display_name", out var d) ? d.GetString() : null,
                username = user.TryGetProperty("username", out var un) ? un.GetString() : null,
                account_id = user.TryGetProperty("account_id", out var a) ? a.GetString() : null,
                uuid = user.TryGetProperty("uuid", out var uuid) ? uuid.GetString() : null,
            });

            if (members.Count >= request.Limit) break;
        }

        return new { members, count = members.Count };
    }
}
