using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.ViewProject;

public sealed class ViewProjectHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewProjectRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        var project = await client.GetAsync<JsonElement>(
            $"workspaces/{workspace}/projects/{Uri.EscapeDataString(request.ProjectKey)}", ct);

        return new
        {
            key = project.TryGetProperty("key", out var k) ? k.GetString() : null,
            name = project.TryGetProperty("name", out var n) ? n.GetString() : null,
            description = project.TryGetProperty("description", out var d) ? d.GetString() : null,
            uuid = project.TryGetProperty("uuid", out var u) ? u.GetString() : null,
            is_private = project.TryGetProperty("is_private", out var p) && p.GetBoolean(),
            created_on = project.TryGetProperty("created_on", out var c) ? c.GetString() : null,
            updated_on = project.TryGetProperty("updated_on", out var up) ? up.GetString() : null,
        };
    }
}
