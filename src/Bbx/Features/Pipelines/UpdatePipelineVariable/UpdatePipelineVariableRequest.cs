namespace Bbx.Features.Pipelines.UpdatePipelineVariable;

public sealed record UpdatePipelineVariableRequest(
    string? Workspace,
    string? Repository,
    string VariableUuid,
    string? Key,
    string? Value,
    bool? Secured);
