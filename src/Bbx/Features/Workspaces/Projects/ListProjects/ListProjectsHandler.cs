using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.ListProjects;

public sealed class ListProjectsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ListProjectsRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        var projects = new List<object>();
        await foreach (var project in client.GetPaginatedAsync<JsonElement>(
            $"workspaces/{workspace}/projects", ct))
        {
            projects.Add(new
            {
                key = project.TryGetProperty("key", out var k) ? k.GetString() : null,
                name = project.TryGetProperty("name", out var n) ? n.GetString() : null,
                description = project.TryGetProperty("description", out var d) ? d.GetString() : null,
                is_private = project.TryGetProperty("is_private", out var p) && p.GetBoolean(),
                created_on = project.TryGetProperty("created_on", out var c) ? c.GetString() : null,
            });

            if (projects.Count >= request.Limit) break;
        }

        return new { workspace, count = projects.Count, projects };
    }
}
