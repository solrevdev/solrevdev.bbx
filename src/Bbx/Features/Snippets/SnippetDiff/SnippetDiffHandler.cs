using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Snippets.SnippetDiff;

public sealed class SnippetDiffHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(SnippetDiffRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        // Raw text, like `pr diff` and `commit patch`, because that is the
        // useful form to pipe into git apply.
        var format = request.AsPatch ? "patch" : "diff";
        return await client.GetRawAsync(
            $"snippets/{workspace}/{request.SnippetId}/{Uri.EscapeDataString(request.Revision)}/{format}", ct);
    }
}
