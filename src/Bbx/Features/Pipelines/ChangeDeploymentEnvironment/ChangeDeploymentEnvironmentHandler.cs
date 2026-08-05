using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ChangeDeploymentEnvironment;

public sealed class ChangeDeploymentEnvironmentHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string NothingToChange =
        "Error: nothing to change. Pass --name, --lock or --unlock.";

    public async Task<string> HandleAsync(ChangeDeploymentEnvironmentRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        // Environments are changed through a changes endpoint rather than a PUT
        // on the environment itself, and it answers 204 with no body.
        var change = new Dictionary<string, object>();
        if (request.Name is not null) change["name"] = request.Name;
        if (request.Lock is not null)
            change["lock"] = new { type = "deployment_environment_lock", locked = request.Lock.Value };
        if (change.Count == 0) throw new BbxUserException(NothingToChange);
        if (request.Reason is not null) change["change_request"] = new { reason = request.Reason };

        await client.PostAsync<JsonElement>(
            $"repositories/{ws}/{repo}/environments/{Uri.EscapeDataString(request.Environment)}/changes",
            change, ct);
        return $"\u2713 Updated deployment environment {request.Environment}";
    }
}
