using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.CreateProject;

public sealed class CreateProjectHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(CreateProjectRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        var payload = new Dictionary<string, object>
        {
            ["name"] = request.Name,
            ["key"] = request.ProjectKey,
            ["is_private"] = request.IsPrivate,
        };

        if (!string.IsNullOrEmpty(request.Description))
            payload["description"] = request.Description;

        var project = await client.PostAsync<JsonElement>(
            $"workspaces/{workspace}/projects", payload, ct);

        return new
        {
            key = project.TryGetProperty("key", out var k) ? k.GetString() : null,
            name = project.TryGetProperty("name", out var n) ? n.GetString() : null,
            description = project.TryGetProperty("description", out var d) ? d.GetString() : null,
            is_private = project.TryGetProperty("is_private", out var p) && p.GetBoolean(),
            created_on = project.TryGetProperty("created_on", out var c) ? c.GetString() : null,
        };
    }
}
