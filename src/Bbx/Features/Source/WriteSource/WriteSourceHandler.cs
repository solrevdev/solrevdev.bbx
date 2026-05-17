using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Source.WriteSource;

public sealed class WriteSourceHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(WriteSourceRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");

        if (request.Files is null || request.Files.Length == 0)
            throw new BbxUserException("Error: At least one --file <local>=<repo-path> mapping is required.");
        if (string.IsNullOrEmpty(request.Branch))
            throw new BbxUserException("Error: --branch is required.");
        if (string.IsNullOrEmpty(request.Message))
            throw new BbxUserException("Error: --message is required.");

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(request.Message), "message");
        content.Add(new StringContent(request.Branch), "branch");
        if (!string.IsNullOrEmpty(request.Author))
            content.Add(new StringContent(request.Author), "author");

        var uploaded = new List<object>();
        foreach (var mapping in request.Files)
        {
            var (localPath, repoPath) = ParseMapping(mapping);
            if (!File.Exists(localPath))
                throw new BbxUserException($"Error: File not found: {localPath}");
            var bytes = await File.ReadAllBytesAsync(localPath, ct);
            // The form-part name is the destination path inside the repo.
            content.Add(new ByteArrayContent(bytes), repoPath, Path.GetFileName(repoPath));
            uploaded.Add(new { local = localPath, repo_path = repoPath, size = bytes.LongLength });
        }

        // POST returns 201 with no body on success — keep parsing tolerant.
        var response = await client.PostMultipartAsync<JsonElement>(
            $"/repositories/{ws}/{repo}/src", content, ct);

        return new
        {
            workspace = ws,
            repository = repo,
            branch = request.Branch,
            message = request.Message,
            author = request.Author,
            files = uploaded,
            response = response.ValueKind == JsonValueKind.Undefined ? null : (object)response,
        };
    }

    private static (string Local, string Repo) ParseMapping(string mapping)
    {
        if (string.IsNullOrEmpty(mapping))
            throw new BbxUserException("Error: Empty --file mapping.");

        var idx = mapping.IndexOf('=');
        if (idx < 0)
            return (mapping, mapping.TrimStart('/'));

        var local = mapping[..idx];
        var repo = mapping[(idx + 1)..].TrimStart('/');
        if (string.IsNullOrEmpty(local) || string.IsNullOrEmpty(repo))
            throw new BbxUserException($"Error: Invalid --file mapping '{mapping}'. Use <local>=<repo-path>.");
        return (local, repo);
    }
}
