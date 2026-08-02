using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.ForkRepo;

public sealed class ForkRepoHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ForkRepoRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.RepoPath(credentials, request.Workspace, request.Repository,
            "Error: Invalid repository path.");

        var body = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(request.Name)) body["name"] = request.Name;
        if (!string.IsNullOrEmpty(request.ToWorkspace)) body["workspace"] = new { slug = request.ToWorkspace };

        return await client.PostAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/forks",
            body.Count > 0 ? body : null,
            ct);
    }
}
