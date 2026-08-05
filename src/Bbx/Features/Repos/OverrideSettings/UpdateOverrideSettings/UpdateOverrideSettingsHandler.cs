using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Repos.OverrideSettings.UpdateOverrideSettings;

public sealed class UpdateOverrideSettingsHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string NothingToUpdate =
        "Error: nothing to update. Pass at least one of --default-reviewers, "
        + "--branching-model or --branch-restrictions, each true or false.";

    public async Task<JsonElement> HandleAsync(UpdateOverrideSettingsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        // Each flag says whether the repository overrides the project-level
        // setting or inherits it. Sending one it was not asked about would
        // silently re-inherit a setting the caller had overridden.
        var body = new Dictionary<string, object>();
        if (request.DefaultReviewers is not null) body["default_reviewers"] = request.DefaultReviewers.Value;
        if (request.BranchingModel is not null) body["branching_model"] = request.BranchingModel.Value;
        if (request.BranchRestrictions is not null) body["branch_restrictions"] = request.BranchRestrictions.Value;

        if (body.Count == 0) throw new BbxUserException(NothingToUpdate);

        return await client.PutAsync<JsonElement>($"repositories/{ws}/{repo}/override-settings", body, ct);
    }
}
