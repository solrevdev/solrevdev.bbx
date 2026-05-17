using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Composition;
using Bbx.Features.Common;

namespace Bbx.Features.Snippets.SnippetFiles;

public sealed class SnippetFilesHandler(BitbucketClient client, CredentialManager credentials)
{
    public async Task HandleAsync(SnippetFilesRequest request, CancellationToken ct)
    {
        if (!credentials.HasCredentials())
            throw new BbxUserException("Error: Not authenticated. Run 'bbx auth login' first.");

        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        if (string.IsNullOrEmpty(request.FileName))
        {
            var snippet = await client.GetAsync<JsonElement>($"snippets/{workspace}/{request.SnippetId}/files", ct);

            var files = new List<object>();
            if (snippet.ValueKind == JsonValueKind.Array)
            {
                foreach (var file in snippet.EnumerateArray())
                {
                    files.Add(new
                    {
                        path = file.TryGetProperty("path", out var p) ? p.GetString() : null,
                        size = file.TryGetProperty("size", out var s) ? s.GetInt64() : 0,
                        mimetype = file.TryGetProperty("mimetype", out var m) ? m.GetString() : null,
                    });
                }
            }

            Console.WriteLine(JsonSerializer.Serialize(new { files, count = files.Count }, JsonOptions.Indented));
        }
        else
        {
            var content = await client.GetRawAsync(
                $"snippets/{workspace}/{request.SnippetId}/files/{request.FileName}", ct);

            if (request.Raw)
            {
                Console.Write(content);
            }
            else
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    file_name = request.FileName,
                    content,
                }, JsonOptions.Indented));
            }
        }
    }
}
