namespace Bbx.Features.Pipelines.CreatePipelineSchedule;

public sealed record CreatePipelineScheduleRequest(
    string? Workspace,
    string? Repository,
    string Cron,
    string? Branch,
    string? Pattern,
    bool Enabled);
