using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Snippets.DeleteSnippet;

public sealed class DeleteSnippetHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(DeleteSnippetRequest request, CancellationToken ct)
    {

        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        await client.DeleteAsync(SnippetPath.For(workspace, request.SnippetId, request.Revision), ct);
        return new { deleted = true, snippet_id = request.SnippetId };
    }
}
