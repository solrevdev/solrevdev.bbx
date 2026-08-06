using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.BranchingModel.EffectiveBranchingModel;

public sealed class EffectiveBranchingModelHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(EffectiveBranchingModelRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        return await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/effective-branching-model", ct);
    }
}
