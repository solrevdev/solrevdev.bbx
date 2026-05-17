using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.BranchingModel.UpdateProjectBranchingModelSettings;

public sealed class UpdateProjectBranchingModelSettingsHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(UpdateProjectBranchingModelSettingsRequest request, CancellationToken ct)
    {
        var ws = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or run: bbx auth set-workspace <workspace>");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");
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
            $"/workspaces/{ws}/projects/{Uri.EscapeDataString(request.ProjectKey)}/branching-model/settings",
            payload, ct);
    }
}
