using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.ChangeDeploymentEnvironment;

public sealed class ChangeDeploymentEnvironmentHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string NothingToChange =
        "Error: nothing to change. Pass --name, --admin-only or --no-admin-only.";

    public async Task<string> HandleAsync(ChangeDeploymentEnvironmentRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        // The body is a change envelope, not a set of fields. The spec documents
        // no body at all, so this shape was read off the Bitbucket web UI, which
        // posts exactly {"change":{"name":"..."}} to this path with a trailing
        // slash. Only name and restrictions can be changed; rank, hidden,
        // environment_type, environment_lock_enabled and lock are all answered
        // with 400 deploy-service.environment.change-not-supported.
        var change = new Dictionary<string, object>();
        if (request.Name is not null) change["name"] = request.Name;
        if (request.AdminOnly is not null)
            change["restrictions"] = new { admin_only = request.AdminOnly.Value };
        if (change.Count == 0) throw new BbxUserException(NothingToChange);

        // Answers 202 with an empty body: the change is queued, not applied inline.
        await client.PostAsync<JsonElement>(
            $"repositories/{ws}/{repo}/environments/{Uri.EscapeDataString(request.Environment)}/changes/",
            new { change }, ct);
        return $"✓ Updated deployment environment {request.Environment}";
    }
}
