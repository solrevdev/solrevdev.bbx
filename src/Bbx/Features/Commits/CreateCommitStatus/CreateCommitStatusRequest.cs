namespace Bbx.Features.Commits.CreateCommitStatus;

public sealed record CreateCommitStatusRequest(
    string? Workspace,
    string? Repository,
    string Hash,
    string Key,
    string State,
    string Url,
    string? Name,
    string? Description);
