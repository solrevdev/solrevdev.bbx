namespace Bbx.Features.Pipelines.DeletePipelineSshKeyPair;

public sealed record DeletePipelineSshKeyPairRequest(
    string? Workspace,
    string? Repository);
