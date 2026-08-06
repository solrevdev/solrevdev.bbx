namespace Bbx.Features.Pipelines.SetPipelineBuildNumber;

public sealed record SetPipelineBuildNumberRequest(
    string? Workspace,
    string? Repository,
    int Next);
