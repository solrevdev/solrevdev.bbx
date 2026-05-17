namespace Bbx.Features.Pipelines.ListTestCases;

public sealed record ListTestCasesRequest(
    string? Workspace,
    string? Repository,
    string PipelineUuid,
    string StepUuid,
    int Limit);
