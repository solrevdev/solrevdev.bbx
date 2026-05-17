using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.CloneRepo;

public sealed class CloneRepoHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(CloneRepoRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.RepoPath(credentials, request.Workspace, request.Repository,
            "Error: Invalid repository path.");

        var result = await client.GetAsync<JsonElement>($"/repositories/{ws}/{repo}", ct);
        if (result.TryGetProperty("links", out var links) && links.TryGetProperty("clone", out var cloneLinks))
        {
            foreach (var link in cloneLinks.EnumerateArray())
            {
                var name = link.GetProperty("name").GetString();
                var href = link.GetProperty("href").GetString();
                if ((request.Ssh && name == "ssh") || (!request.Ssh && name == "https"))
                    return href ?? throw new BbxUserException("Error: Clone URL not found");
            }
        }
        throw new BbxUserException("Error: Clone URL not found");
    }
}
