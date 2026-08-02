using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Workspaces.Hooks.DeleteWorkspaceHook;

public sealed class DeleteWorkspaceHookHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteWorkspaceHookRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        await client.DeleteAsync(
            $"workspaces/{workspace}/hooks/{Uri.EscapeDataString(request.Uid)}", ct);
        return $"✓ Deleted webhook '{request.Uid}' from workspace {workspace}";
    }
}
