namespace Bbx.Features.Pipelines.ViewPipelineVariable;

public sealed record ViewPipelineVariableRequest(
    string? Workspace,
    string? Repository,
    string VariableUuid);
