using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Issues.CreateIssue;

public sealed class CreateIssueHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(CreateIssueRequest request, CancellationToken ct)
    {
        DeprecationNotice.Emit();
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var body = new Dictionary<string, object> { ["title"] = request.Title };
        if (!string.IsNullOrEmpty(request.Content)) body["content"] = new { raw = request.Content };
        if (!string.IsNullOrEmpty(request.Kind)) body["kind"] = request.Kind;
        if (!string.IsNullOrEmpty(request.Priority)) body["priority"] = request.Priority;

        return await client.PostAsync<JsonElement>($"/repositories/{ws}/{repo}/issues", body, ct);
    }
}
