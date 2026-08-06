using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.Pipelines;

namespace Bbx.Features.Workspaces.Pipelines.DeleteWorkspaceVariable;

public sealed class DeleteWorkspaceVariableHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(DeleteWorkspaceVariableRequest request, CancellationToken ct)
    {
        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");
        var endpoint = $"workspaces/{workspace}/pipelines-config/variables";
        await client.DeleteAsync($"{endpoint}/{Uri.EscapeDataString(request.VariableUuid)}", ct);
        return $"\u2713 Deleted workspace pipeline variable {request.VariableUuid}";
    }
}
