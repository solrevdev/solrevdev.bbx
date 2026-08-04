namespace Bbx.Features.Common;

internal static class EndpointPath
{
    public static string EscapeSegments(string? path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;

        return string.Join('/', path.TrimStart('/').Split('/').Select(Uri.EscapeDataString));
    }
}
