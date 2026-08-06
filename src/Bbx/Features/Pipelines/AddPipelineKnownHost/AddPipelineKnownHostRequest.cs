namespace Bbx.Features.Pipelines.AddPipelineKnownHost;

public sealed record AddPipelineKnownHostRequest(
    string? Workspace,
    string? Repository,
    string Hostname,
    string KeyType,
    string Key);
