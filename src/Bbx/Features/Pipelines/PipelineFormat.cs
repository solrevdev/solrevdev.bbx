using System.Text.Json;

namespace Bbx.Features.Pipelines;

internal static class PipelineFormat
{
    public static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    public static string BuildQuery(string? status, string? branch)
    {
        var conditions = new List<string>();
        if (!string.IsNullOrEmpty(status))
            conditions.Add($"state.name=\"{status.ToUpperInvariant()}\"");
        if (!string.IsNullOrEmpty(branch))
            conditions.Add($"target.ref_name=\"{branch}\"");
        return string.Join(" AND ", conditions);
    }

    public static object Pipeline(JsonElement p) => new
    {
        uuid = GetString(p, "uuid"),
        build_number = p.TryGetProperty("build_number", out var bn) ? bn.GetInt32() : 0,
        state = p.TryGetProperty("state", out var state) ? (object)new
        {
            name = GetString(state, "name"),
            result = state.TryGetProperty("result", out var r) ? GetString(r, "name") : null,
        } : null!,
        target = p.TryGetProperty("target", out var target) ? (object)new
        {
            ref_type = GetString(target, "ref_type"),
            ref_name = GetString(target, "ref_name"),
            commit = target.TryGetProperty("commit", out var c) ? GetString(c, "hash") : null,
        } : null!,
        trigger = p.TryGetProperty("trigger", out var trigger) ? GetString(trigger, "name") : null,
        created_on = GetString(p, "created_on"),
        completed_on = GetString(p, "completed_on"),
        duration_in_seconds = p.TryGetProperty("duration_in_seconds", out var dur) ? dur.GetInt32() : (int?)null,
    };

    public static object PipelineDetailed(JsonElement p)
    {
        var basic = Pipeline(p);
        return new
        {
            ((dynamic)basic).uuid,
            ((dynamic)basic).build_number,
            ((dynamic)basic).state,
            ((dynamic)basic).target,
            ((dynamic)basic).trigger,
            ((dynamic)basic).created_on,
            ((dynamic)basic).completed_on,
            ((dynamic)basic).duration_in_seconds,
            creator = p.TryGetProperty("creator", out var creator) ? (object)new
            {
                display_name = GetString(creator, "display_name"),
                account_id = GetString(creator, "account_id"),
            } : null!,
            repository = p.TryGetProperty("repository", out var repo) ? (object)new
            {
                name = GetString(repo, "name"),
                full_name = GetString(repo, "full_name"),
            } : null!,
            links = p.TryGetObject("links", out var links) && links.TryGetProperty("html", out var html)
                ? GetString(html, "href") : null,
        };
    }

    public static object Step(JsonElement s) => new
    {
        uuid = GetString(s, "uuid"),
        name = GetString(s, "name"),
        state = s.TryGetProperty("state", out var state) ? (object)new
        {
            name = GetString(state, "name"),
            result = state.TryGetProperty("result", out var r) ? GetString(r, "name") : null,
        } : null!,
        started_on = GetString(s, "started_on"),
        completed_on = GetString(s, "completed_on"),
        duration_in_seconds = s.TryGetProperty("duration_in_seconds", out var dur) ? dur.GetInt32() : (int?)null,
        run_number = s.TryGetProperty("run_number", out var rn) ? rn.GetInt32() : (int?)null,
        max_time = s.TryGetProperty("max_time", out var mt) ? mt.GetInt32() : (int?)null,
    };
}
