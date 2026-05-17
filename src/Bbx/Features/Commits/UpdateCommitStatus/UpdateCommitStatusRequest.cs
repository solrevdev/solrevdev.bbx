namespace Bbx.Features.Commits.UpdateCommitStatus;

public sealed record UpdateCommitStatusRequest(
    string? Workspace,
    string? Repository,
    string Hash,
    string Key,
    string? State,
    string? Url,
    string? Name,
    string? Description);
