using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.CreateRepo;

public sealed class CreateRepoHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(CreateRepoRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace, "Error: Workspace required.");

        var body = new Dictionary<string, object>
        {
            ["scm"] = "git",
            ["is_private"] = request.IsPrivate,
            ["name"] = request.Name,
        };
        if (!string.IsNullOrEmpty(request.Description)) body["description"] = request.Description;
        if (!string.IsNullOrEmpty(request.Project)) body["project"] = new { key = request.Project };
        if (!string.IsNullOrEmpty(request.ForkPolicy)) body["fork_policy"] = request.ForkPolicy;

        var slug = request.Name.ToLowerInvariant().Replace(' ', '-');
        return await client.PostAsync<JsonElement>($"/repositories/{workspace}/{slug}", body, ct);
    }
}
