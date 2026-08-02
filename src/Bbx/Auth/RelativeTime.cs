namespace Bbx.Auth;

internal static class RelativeTime
{
    public static string DescribeFuture(DateTimeOffset target)
    {
        var span = target - DateTimeOffset.UtcNow;
        if (span <= TimeSpan.Zero) return "expired";
        if (span.TotalSeconds < 60) return $"in {(int)span.TotalSeconds}s";
        if (span.TotalMinutes < 60) return $"in {(int)span.TotalMinutes}m";
        if (span.TotalHours < 48) return $"in {span.TotalHours:0.#}h";
        return $"in {span.TotalDays:0.#}d";
    }
}
