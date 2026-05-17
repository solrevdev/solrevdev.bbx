using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.ViewRepo;

public sealed class ViewRepoHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewRepoRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.RepoPath(credentials, request.Workspace, request.Repository,
            "Error: Invalid repository path. Use workspace/repo or set workspace.");
        return await client.GetAsync<JsonElement>($"/repositories/{ws}/{repo}", ct);
    }
}
