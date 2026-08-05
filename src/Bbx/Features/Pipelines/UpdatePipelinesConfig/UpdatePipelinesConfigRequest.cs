namespace Bbx.Features.Pipelines.UpdatePipelinesConfig;

public sealed record UpdatePipelinesConfigRequest(
    string? Workspace,
    string? Repository,
    bool Enabled);
