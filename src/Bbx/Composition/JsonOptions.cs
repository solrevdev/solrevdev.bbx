using System.Text.Json;

namespace Bbx.Composition;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Indented = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
    };
}
