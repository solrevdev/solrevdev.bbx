namespace Bbx.Features.Pipelines.ListTestCaseReasons;

public sealed record ListTestCaseReasonsRequest(
    string? Workspace,
    string? Repository,
    string PipelineUuid,
    string StepUuid,
    string TestCaseUuid,
    int Limit);
