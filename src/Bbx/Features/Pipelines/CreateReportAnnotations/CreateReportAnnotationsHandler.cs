using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.CreateReportAnnotations;

public sealed class CreateReportAnnotationsHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string NotAnArray =
        "Error: --annotations must be a JSON array of annotation objects.";

    public async Task<object> HandleAsync(CreateReportAnnotationsRequest request, CancellationToken ct)
    {
        var (ws, repo) = Resolve.WorkspaceAndRepo(credentials, request.Workspace, request.Repository,
            "Error: Workspace and repository required.");
        var report = $"repositories/{ws}/{repo}/commit/{request.Hash}"
            + $"/reports/{Uri.EscapeDataString(request.ReportId)}";

        // This endpoint takes a bare array rather than an object, and creates
        // every annotation in one call. Passing raw JSON keeps the whole
        // annotation shape reachable without inventing a flag per field.
        JsonElement payload;
        try
        {
            payload = JsonDocument.Parse(request.AnnotationsJson).RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new BbxUserException($"Error: --annotations is not valid JSON: {ex.Message}");
        }
        if (payload.ValueKind != JsonValueKind.Array) throw new BbxUserException(NotAnArray);

        var created = await client.PostAsync<JsonElement>($"{report}/annotations", payload, ct);
        return new { report_id = request.ReportId, annotations = created };
    }
}
