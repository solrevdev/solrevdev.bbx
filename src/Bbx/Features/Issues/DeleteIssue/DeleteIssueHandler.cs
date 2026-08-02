using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Issues.DeleteIssue;

public sealed class DeleteIssueHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteIssueRequest request, CancellationToken ct)
    {
        DeprecationNotice.Emit();
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.DeleteAsync($"/repositories/{ws}/{repo}/issues/{request.Id}", ct);
        return $"✓ Deleted issue #{request.Id}";
    }
}
