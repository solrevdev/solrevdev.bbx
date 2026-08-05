using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.DeletePipelineSshKeyPair;

public sealed class DeletePipelineSshKeyPairHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeletePipelineSshKeyPairRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        await client.DeleteAsync($"repositories/{ws}/{repo}/pipelines_config/ssh/key_pair", ct);
        return $"\u2713 Deleted the pipelines SSH key pair for {ws}/{repo}";
    }
}
