using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Issues.ViewIssue;

public sealed class ViewIssueHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewIssueRequest request, CancellationToken ct)
    {
        DeprecationNotice.Emit();
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        return await client.GetAsync<JsonElement>($"/repositories/{ws}/{repo}/issues/{request.Id}", ct);
    }
}
