using System.Text.Json;

namespace Bbx.Composition;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Indented = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
    };

    public static readonly JsonSerializerOptions Compact = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
    };

    /// <summary>
    /// When true, <see cref="Current"/> returns <see cref="Compact"/> (no
    /// whitespace, single-line). Default false → pretty-printed.
    /// Toggled by <c>--json-compact</c> on the command line or
    /// <c>BBX_JSON_COMPACT=1|true</c> in the environment, both honoured by
    /// <c>Program.Main</c>.
    /// </summary>
    public static bool UseCompact { get; set; }

    public static JsonSerializerOptions Current => UseCompact ? Compact : Indented;
}
