using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.OverrideSettings.ViewOverrideSettings;

public sealed class ViewOverrideSettingsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewOverrideSettingsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        return await client.GetAsync<JsonElement>($"repositories/{ws}/{repo}/override-settings", ct);
    }
}
