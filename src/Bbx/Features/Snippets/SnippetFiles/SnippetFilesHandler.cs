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

        var workspace = Resolve.Workspace(credentials, request.Workspace,
            "Error: Workspace required. Use --workspace or set default with 'bbx auth set-workspace'.");

        if (string.IsNullOrEmpty(request.FileName))
        {
            // There is no /files collection endpoint (it 404s). The file list is
            // a "files" object on the snippet itself, keyed by path.
            var snippet = await client.GetAsync<JsonElement>($"snippets/{workspace}/{request.SnippetId}", ct);

            var files = new List<object>();
            if (snippet.TryGetObject("files", out var fileMap))
            {
                foreach (var file in fileMap.EnumerateObject())
                {
                    files.Add(new
                    {
                        path = file.Name,
                        links = file.Value.TryGetObject("links", out var l)
                                && l.TryGetObject("self", out var self)
                            ? self.GetStringOrNull("href")
                            : null,
                    });
                }
            }

            Console.WriteLine(JsonSerializer.Serialize(new { files, count = files.Count }, JsonOptions.Indented));
        }
        else
        {
            var content = await client.GetRawAsync(
                $"snippets/{workspace}/{request.SnippetId}/files/{EndpointPath.EscapeSegments(request.FileName)}", ct);

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
