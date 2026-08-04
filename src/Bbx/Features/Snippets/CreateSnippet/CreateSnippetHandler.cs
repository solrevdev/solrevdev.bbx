using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Snippets.CreateSnippet;

public sealed class CreateSnippetHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task<object> HandleAsync(CreateSnippetRequest request, CancellationToken ct)
    {

        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(request.Title), "title");
        content.Add(new StringContent(request.IsPrivate.ToString().ToLower()), "is_private");

        foreach (var filePath in request.Files)
        {
            if (!File.Exists(filePath))
                throw new BbxUserException($"Error: File not found: {filePath}");

            var fileName = Path.GetFileName(filePath);
            var fileContent = await File.ReadAllBytesAsync(filePath, ct);
            content.Add(new ByteArrayContent(fileContent), "file", fileName);
        }

        var snippet = await client.PostMultipartAsync<JsonElement>($"snippets/{workspace}", content, ct);

        return new
        {
            id = snippet.GetProperty("id").GetString(),
            title = snippet.TryGetProperty("title", out var t) ? t.GetString() : null,
            is_private = snippet.TryGetProperty("is_private", out var p) && p.GetBoolean(),
            created_on = snippet.TryGetProperty("created_on", out var c) ? c.GetString() : null,
            links = snippet.TryGetObject("links", out var l) && l.TryGetObject("html", out var h) && h.TryGetProperty("href", out var href)
                ? href.GetString() : null,
        };
    }
}
