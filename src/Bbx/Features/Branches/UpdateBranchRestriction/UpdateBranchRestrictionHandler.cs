using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Branches.UpdateBranchRestriction;

public sealed class UpdateBranchRestrictionHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string NothingToUpdate =
        "Error: nothing to update. Pass at least one of --pattern, --value, --users or --groups.";

    public async Task<JsonElement> HandleAsync(UpdateBranchRestrictionRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var body = new Dictionary<string, object>();
        if (request.Pattern is not null) body["pattern"] = request.Pattern;
        if (request.Value is not null) body["value"] = request.Value.Value;
        // An absent array option parses to an empty array rather than null, so
        // keying off null would send an empty exemption list on every edit and
        // strip the exemptions off the rule.
        if (request.Users is { Length: > 0 })
            body["users"] = request.Users.Select(u => new { uuid = u }).ToArray();
        if (request.Groups is { Length: > 0 })
            body["groups"] = request.Groups.Select(g => new { slug = g }).ToArray();

        if (body.Count == 0) throw new BbxUserException(NothingToUpdate);

        return await client.PutAsync<JsonElement>(
            $"repositories/{ws}/{repo}/branch-restrictions/{request.Id}", body, ct);
    }
}
