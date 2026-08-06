namespace Bbx.Features.Pipelines.ViewPipelineSchedule;

public sealed record ViewPipelineScheduleRequest(
    string? Workspace,
    string? Repository,
    string ScheduleUuid);
