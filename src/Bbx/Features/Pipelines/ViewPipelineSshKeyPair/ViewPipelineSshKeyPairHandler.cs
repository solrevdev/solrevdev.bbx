using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ViewPipelineSshKeyPair;

public sealed class ViewPipelineSshKeyPairHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(ViewPipelineSshKeyPairRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        var pair = await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pipelines_config/ssh/key_pair", ct);
        // The private half is write-only: Bitbucket never returns it, and this
        // does not go looking for it.
        return new
        {
            public_key = PipelineFormat.GetString(pair, "public_key"),
            type = PipelineFormat.GetString(pair, "type"),
        };
    }
}
