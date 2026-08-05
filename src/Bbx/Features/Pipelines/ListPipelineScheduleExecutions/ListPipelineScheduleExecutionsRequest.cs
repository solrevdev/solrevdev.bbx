namespace Bbx.Features.Pipelines.ListPipelineScheduleExecutions;

public sealed record ListPipelineScheduleExecutionsRequest(
    string? Workspace,
    string? Repository,
    string ScheduleUuid,
    int Limit);
