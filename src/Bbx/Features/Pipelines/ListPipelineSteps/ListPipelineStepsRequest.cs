namespace Bbx.Features.Pipelines.ListPipelineSteps;

public sealed record ListPipelineStepsRequest(string? Workspace, string? Repository, string PipelineUuid);
