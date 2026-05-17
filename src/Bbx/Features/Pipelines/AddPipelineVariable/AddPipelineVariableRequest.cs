namespace Bbx.Features.Pipelines.AddPipelineVariable;

public sealed record AddPipelineVariableRequest(
    string? Workspace,
    string? Repository,
    string Key,
    string Value,
    bool Secured);
