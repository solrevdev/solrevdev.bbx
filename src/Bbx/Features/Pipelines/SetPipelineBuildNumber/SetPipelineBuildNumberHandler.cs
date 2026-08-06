using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.SetPipelineBuildNumber;

public sealed class SetPipelineBuildNumberHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string MustBeAhead =
        "Error: --next must be greater than zero. Bitbucket only moves the build number forwards.";

    public async Task<JsonElement> HandleAsync(SetPipelineBuildNumberRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        if (request.Next < 1) throw new BbxUserException(MustBeAhead);

        return await client.PutAsync<JsonElement>(
            $"repositories/{ws}/{repo}/pipelines_config/build_number",
            new { next = request.Next }, ct);
    }
}
