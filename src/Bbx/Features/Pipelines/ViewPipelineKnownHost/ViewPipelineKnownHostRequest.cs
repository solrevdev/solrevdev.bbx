namespace Bbx.Features.Pipelines.ViewPipelineKnownHost;

public sealed record ViewPipelineKnownHostRequest(
    string? Workspace,
    string? Repository,
    string HostUuid);
