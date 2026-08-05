namespace Bbx.Features.Pipelines.SetPipelineSshKeyPair;

public sealed record SetPipelineSshKeyPairRequest(
    string? Workspace,
    string? Repository,
    string PrivateKey,
    string PublicKey);
