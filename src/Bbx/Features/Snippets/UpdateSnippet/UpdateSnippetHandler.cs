using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Snippets.UpdateSnippet;

public sealed class UpdateSnippetHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(UpdateSnippetRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");

        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        using var content = new MultipartFormDataContent();

        if (!string.IsNullOrEmpty(request.Title))
            content.Add(new StringContent(request.Title), "title");

        if (request.IsPrivate.HasValue)
            content.Add(new StringContent(request.IsPrivate.Value.ToString().ToLower()), "is_private");

        if (request.Files is not null)
        {
            foreach (var filePath in request.Files)
            {
                if (!File.Exists(filePath))
                    throw new BbxUserException($"Error: File not found: {filePath}");

                var fileName = Path.GetFileName(filePath);
                var fileContent = await File.ReadAllBytesAsync(filePath, ct);
                content.Add(new ByteArrayContent(fileContent), "file", fileName);
            }
        }

        var snippet = await client.PutMultipartAsync<JsonElement>(
            $"snippets/{workspace}/{request.SnippetId}", content, ct);

        return new
        {
            id = snippet.GetProperty("id").GetString(),
            title = snippet.TryGetProperty("title", out var t) ? t.GetString() : null,
            is_private = snippet.TryGetProperty("is_private", out var p) && p.GetBoolean(),
            updated_on = snippet.TryGetProperty("updated_on", out var u) ? u.GetString() : null,
        };
    }
}
