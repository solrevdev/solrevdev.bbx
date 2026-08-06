using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Commits.Comments.AddCommitComment;

public sealed class AddCommitCommentHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string LineNeedsPath = "Error: --line needs --path to say which file the line is in.";

    public async Task<JsonElement> HandleAsync(AddCommitCommentRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        if (request.Line is not null && string.IsNullOrEmpty(request.Path))
            throw new BbxUserException(LineNeedsPath);

        var body = new Dictionary<string, object>
        {
            ["content"] = new { raw = request.Body },
        };
        // Anchoring is set once, at creation. "to" is the line in the file after
        // the commit, which is the one a reviewer means.
        if (!string.IsNullOrEmpty(request.Path))
        {
            body["inline"] = request.Line is null
                ? new Dictionary<string, object> { ["path"] = request.Path }
                : new Dictionary<string, object> { ["path"] = request.Path, ["to"] = request.Line.Value };
        }

        return await client.PostAsync<JsonElement>(
            $"repositories/{ws}/{repo}/commit/{request.Hash}/comments", body, ct);
    }
}
