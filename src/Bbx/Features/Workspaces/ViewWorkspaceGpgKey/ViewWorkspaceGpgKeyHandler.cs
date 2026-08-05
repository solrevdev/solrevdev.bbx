using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.ViewWorkspaceGpgKey;

public sealed class ViewWorkspaceGpgKeyHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewWorkspaceGpgKeyRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
        // The key Bitbucket signs its own web-edit commits with, so a client
        // can verify them.
        return await client.GetAsync<JsonElement>(
            $"workspaces/{workspace}/settings/gpg/public-key", ct);
    }
}
