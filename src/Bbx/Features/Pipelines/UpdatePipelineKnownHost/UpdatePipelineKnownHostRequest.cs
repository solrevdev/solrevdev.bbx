namespace Bbx.Features.Pipelines.UpdatePipelineKnownHost;

public sealed record UpdatePipelineKnownHostRequest(
    string? Workspace,
    string? Repository,
    string HostUuid,
    string Hostname,
    string KeyType,
    string Key);
