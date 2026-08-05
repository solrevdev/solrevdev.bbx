namespace Bbx.Features.Pipelines.ViewPipelineStep;

public sealed record ViewPipelineStepRequest(
    string? Workspace,
    string? Repository,
    string PipelineUuid,
    string StepUuid);
