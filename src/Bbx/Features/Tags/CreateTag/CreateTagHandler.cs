using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Tags.CreateTag;

public sealed class CreateTagHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(CreateTagRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var body = new Dictionary<string, object>
        {
            ["name"] = request.Name,
            ["target"] = new { hash = request.Target },
        };
        if (!string.IsNullOrEmpty(request.Message))
            body["message"] = request.Message;

        return await client.PostAsync<JsonElement>($"/repositories/{ws}/{repo}/refs/tags", body, ct);
    }
}
