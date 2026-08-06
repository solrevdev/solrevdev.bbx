using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.ViewCommitStatus;

public sealed class ViewCommitStatusHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<JsonElement> HandleAsync(ViewCommitStatusRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        return await client.GetAsync<JsonElement>(
            $"repositories/{ws}/{repo}/commit/{request.Hash}/statuses/build/{Uri.EscapeDataString(request.Key)}",
            ct);
    }
}
