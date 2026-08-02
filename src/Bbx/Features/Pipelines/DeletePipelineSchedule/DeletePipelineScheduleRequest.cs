namespace Bbx.Features.Pipelines.DeletePipelineSchedule;

public sealed record DeletePipelineScheduleRequest(string? Workspace, string? Repository, string Uuid);
