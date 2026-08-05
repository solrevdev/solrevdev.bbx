namespace Bbx.Features.Pipelines.UpdatePipelineSchedule;

public sealed record UpdatePipelineScheduleRequest(
    string? Workspace,
    string? Repository,
    string ScheduleUuid,
    bool? Enabled,
    string? Cron);
