using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.SetPipelineSshKeyPair;

public sealed class SetPipelineSshKeyPairHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(SetPipelineSshKeyPairRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        var pair = await client.PutAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pipelines_config/ssh/key_pair",
            new { type = "pipeline_ssh_key_pair", private_key = request.PrivateKey, public_key = request.PublicKey },
            ct);

        return new
        {
            message = "SSH key pair set",
            public_key = PipelineFormat.GetString(pair, "public_key"),
        };
    }
}
