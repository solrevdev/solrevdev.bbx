using System.Text.Json;

namespace Bbx.Features.Pipelines;

internal static class PipelineFormat
{
    public static string? GetString(JsonElement element, string propertyName) =>
        element.GetStringOrNull(propertyName);

    public static string BuildQuery(string? status, string? branch)
    {
        var conditions = new List<string>();
        if (!string.IsNullOrEmpty(status))
            conditions.Add($"state.name=\"{status.ToUpperInvariant()}\"");
        if (!string.IsNullOrEmpty(branch))
            conditions.Add($"target.ref_name=\"{branch}\"");
        return string.Join(" AND ", conditions);
    }

    public static PipelineSummary Pipeline(JsonElement pipeline)
    {
        var state = State(pipeline);
        var target = Target(pipeline);

        return new PipelineSummary(
            GetString(pipeline, "uuid"),
            GetInt32(pipeline, "build_number") ?? 0,
            state,
            target,
            pipeline.TryGetObject("trigger", out var trigger) ? GetString(trigger, "name") : null,
            GetString(pipeline, "created_on"),
            GetString(pipeline, "completed_on"),
            GetInt32(pipeline, "duration_in_seconds"));
    }

    public static PipelineDetails PipelineDetailed(JsonElement pipeline)
    {
        var summary = Pipeline(pipeline);
        PipelineActor? creator = pipeline.TryGetObject("creator", out var creatorElement)
            ? new PipelineActor(
                GetString(creatorElement, "display_name"),
                GetString(creatorElement, "account_id"))
            : null;
        PipelineRepository? repository = pipeline.TryGetObject("repository", out var repositoryElement)
            ? new PipelineRepository(
                GetString(repositoryElement, "name"),
                GetString(repositoryElement, "full_name"))
            : null;
        var link = pipeline.TryGetObject("links", out var links)
                   && links.TryGetObject("html", out var html)
            ? GetString(html, "href")
            : null;

        return new PipelineDetails(
            summary.Uuid,
            summary.BuildNumber,
            summary.State,
            summary.Target,
            summary.Trigger,
            summary.CreatedOn,
            summary.CompletedOn,
            summary.DurationInSeconds,
            creator,
            repository,
            link);
    }

    public static PipelineStep Step(JsonElement step) => new(
        GetString(step, "uuid"),
        GetString(step, "name"),
        State(step),
        GetString(step, "started_on"),
        GetString(step, "completed_on"),
        GetInt32(step, "duration_in_seconds"),
        GetInt32(step, "run_number"),
        GetInt32(step, "max_time"));

    private static PipelineState? State(JsonElement element)
    {
        if (!element.TryGetObject("state", out var state)) return null;

        var result = state.TryGetObject("result", out var resultElement)
            ? GetString(resultElement, "name")
            : null;
        return new PipelineState(GetString(state, "name"), result);
    }

    private static PipelineTarget? Target(JsonElement element)
    {
        if (!element.TryGetObject("target", out var target)) return null;

        var commit = target.TryGetObject("commit", out var commitElement)
            ? GetString(commitElement, "hash")
            : null;
        return new PipelineTarget(
            GetString(target, "ref_type"),
            GetString(target, "ref_name"),
            commit);
    }

    private static int? GetInt32(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt32(out var number)
            ? number
            : null;
}

internal sealed record PipelineState(string? Name, string? Result);

internal sealed record PipelineTarget(string? RefType, string? RefName, string? Commit);

internal sealed record PipelineSummary(
    string? Uuid,
    int BuildNumber,
    PipelineState? State,
    PipelineTarget? Target,
    string? Trigger,
    string? CreatedOn,
    string? CompletedOn,
    int? DurationInSeconds);

internal sealed record PipelineActor(string? DisplayName, string? AccountId);

internal sealed record PipelineRepository(string? Name, string? FullName);

internal sealed record PipelineDetails(
    string? Uuid,
    int BuildNumber,
    PipelineState? State,
    PipelineTarget? Target,
    string? Trigger,
    string? CreatedOn,
    string? CompletedOn,
    int? DurationInSeconds,
    PipelineActor? Creator,
    PipelineRepository? Repository,
    string? Links);

internal sealed record PipelineStep(
    string? Uuid,
    string? Name,
    PipelineState? State,
    string? StartedOn,
    string? CompletedOn,
    int? DurationInSeconds,
    int? RunNumber,
    int? MaxTime);
