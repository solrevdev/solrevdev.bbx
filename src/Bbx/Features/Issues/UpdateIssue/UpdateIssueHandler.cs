using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Issues.UpdateIssue;

public sealed class UpdateIssueHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(UpdateIssueRequest request, CancellationToken ct)
    {
        DeprecationNotice.Emit();
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var body = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(request.Title)) body["title"] = request.Title;
        if (!string.IsNullOrEmpty(request.State)) body["state"] = request.State;
        if (!string.IsNullOrEmpty(request.Priority)) body["priority"] = request.Priority;
        if (!string.IsNullOrEmpty(request.Assignee)) body["assignee"] = new { account_id = request.Assignee };

        return await client.PutAsync<JsonElement>($"/repositories/{ws}/{repo}/issues/{request.Id}", body, ct);
    }
}
