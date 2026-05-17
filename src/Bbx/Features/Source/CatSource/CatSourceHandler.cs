using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Source.CatSource;

public sealed class CatSourceHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<string> HandleAsync(CatSourceRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.Path))
            throw new BbxUserException("Error: <path> is required for 'src cat'.");

        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        var refSegment = Uri.EscapeDataString(request.Ref);
        var path = NormalizePath(request.Path);
        return await client.GetStringAsync($"/repositories/{ws}/{repo}/src/{refSegment}/{path}", ct);
    }

    private static string NormalizePath(string path)
    {
        var trimmed = path.TrimStart('/');
        var parts = trimmed.Split('/');
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = Uri.EscapeDataString(parts[i]);
        }
        return string.Join('/', parts);
    }
}
