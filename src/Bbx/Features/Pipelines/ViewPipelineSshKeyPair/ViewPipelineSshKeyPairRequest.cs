namespace Bbx.Features.Pipelines.ViewPipelineSshKeyPair;

public sealed record ViewPipelineSshKeyPairRequest(
    string? Workspace,
    string? Repository);
