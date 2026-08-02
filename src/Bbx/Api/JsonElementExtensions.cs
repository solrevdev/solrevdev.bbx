using System.Text.Json;

namespace Bbx.Api;

/// <summary>
/// Helpers for reading Bitbucket API payloads.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Get a property that is expected to hold a nested object.
    /// </summary>
    /// <remarks>
    /// <see cref="JsonElement.TryGetProperty(string, out JsonElement)"/> returns
    /// true when the property exists but holds JSON <c>null</c>, and reading a
    /// property off that element throws <see cref="InvalidOperationException"/>.
    /// Bitbucket sends explicit nulls for absent nested objects: a lightweight
    /// tag has <c>"tagger": null</c>, which crashed <c>bbx branch tag list</c>.
    /// This overload returns false for null, for non-object values, and when the
    /// element it is called on is not itself an object.
    /// </remarks>
    public static bool TryGetObject(this JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var candidate)
            && candidate.ValueKind == JsonValueKind.Object)
        {
            value = candidate;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Read a string property, treating JSON <c>null</c> and non-string values as absent.
    /// </summary>
    public static string? GetStringOrNull(this JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object
           && element.TryGetProperty(name, out var value)
           && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
