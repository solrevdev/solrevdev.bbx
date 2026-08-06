namespace Bbx.Features.Pipelines.DeletePipelineKnownHost;

public sealed record DeletePipelineKnownHostRequest(
    string? Workspace,
    string? Repository,
    string HostUuid);
