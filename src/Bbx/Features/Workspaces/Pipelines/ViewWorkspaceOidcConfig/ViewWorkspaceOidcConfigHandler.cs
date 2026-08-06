using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Pipelines.ViewWorkspaceOidcConfig;

public sealed class ViewWorkspaceOidcConfigHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewWorkspaceOidcConfigRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
        return await client.GetAsync<JsonElement>(
            $"workspaces/{workspace}/pipelines-config/identity/oidc/.well-known/openid-configuration", ct);
    }
}
