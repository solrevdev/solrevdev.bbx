namespace Bbx.Features.Pipelines.DeletePipelineVariable;

public sealed record DeletePipelineVariableRequest(string? Workspace, string? Repository, string Uuid);
