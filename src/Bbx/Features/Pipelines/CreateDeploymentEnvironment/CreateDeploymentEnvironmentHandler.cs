using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.CreateDeploymentEnvironment;

public sealed class CreateDeploymentEnvironmentHandler(BitbucketClient client, CredentialManager credentials)
{
    public static readonly string[] Types = ["Test", "Staging", "Production"];

    public async Task<JsonElement> HandleAsync(CreateDeploymentEnvironmentRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");
        if (!Types.Contains(request.EnvironmentType, StringComparer.OrdinalIgnoreCase))
            throw new BbxUserException($"Error: --type must be one of {string.Join(", ", Types)}.");

        // Bitbucket needs the rank alongside the type name: it orders the
        // environments in the deployment view and a body without it is refused.
        var body = new
        {
            type = "deployment_environment",
            name = request.Name,
            environment_type = new { type = "deployment_environment_type", name = request.EnvironmentType, rank = request.Rank },
            rank = request.Rank,
        };

        return await client.PostAsync<JsonElement>($"repositories/{ws}/{repo}/environments", body, ct);
    }
}
