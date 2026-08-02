using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Tags.DeleteTag;

public sealed class DeleteTagHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteTagRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        await client.DeleteAsync(
            $"/repositories/{ws}/{repo}/refs/tags/{Uri.EscapeDataString(request.Name)}", ct);
        return $"✓ Deleted tag '{request.Name}' from {ws}/{repo}";
    }
}
