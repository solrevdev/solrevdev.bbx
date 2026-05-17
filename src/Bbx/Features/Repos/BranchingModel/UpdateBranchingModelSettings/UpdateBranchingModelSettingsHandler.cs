using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.BranchingModel.UpdateBranchingModelSettings;

public sealed class UpdateBranchingModelSettingsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(UpdateBranchingModelSettingsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        if (string.IsNullOrWhiteSpace(request.SettingsJson))
            throw new BbxUserException(
                "Error: --settings <json> is required (the branching-model settings payload).");

        JsonElement payload;
        try
        {
            payload = JsonDocument.Parse(request.SettingsJson).RootElement;
        }
        catch (JsonException ex)
        {
            throw new BbxUserException($"Error: --settings is not valid JSON: {ex.Message}");
        }

        return await client.PutAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/branching-model/settings", payload, ct);
    }
}
