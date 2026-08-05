using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Projects.UpdateProject;

public sealed class UpdateProjectHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string NothingToUpdate =
        "Error: nothing to update. Pass --name, --description, --private, --public or --new-key.";

    public async Task<JsonElement> HandleAsync(UpdateProjectRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
        if (string.IsNullOrEmpty(request.ProjectKey))
            throw new BbxUserException("Error: --project-key is required.");
        var key = Uri.EscapeDataString(request.ProjectKey);

        var body = new Dictionary<string, object>();
        if (request.Name is not null) body["name"] = request.Name;
        if (request.Description is not null) body["description"] = request.Description;
        if (request.IsPrivate is not null) body["is_private"] = request.IsPrivate.Value;
        // Renaming the key moves every repository in the project, so it is its
        // own switch rather than a positional the caller might mistype.
        if (request.NewKey is not null) body["key"] = request.NewKey;

        if (body.Count == 0) throw new BbxUserException(NothingToUpdate);

        return await client.PutAsync<JsonElement>($"workspaces/{workspace}/projects/{key}", body, ct);
    }
}
