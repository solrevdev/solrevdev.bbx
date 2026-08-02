using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Branches.CreateBranch;

public sealed class CreateBranchHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(CreateBranchRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        var body = new
        {
            name = request.Name,
            target = new { hash = request.Target },
        };
        return await client.PostAsync<JsonElement>($"/repositories/{ws}/{repo}/refs/branches", body, ct);
    }
}
