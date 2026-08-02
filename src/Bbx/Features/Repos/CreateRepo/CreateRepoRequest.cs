namespace Bbx.Features.Repos.CreateRepo;

public sealed record CreateRepoRequest(
    string? Workspace,
    string Name,
    bool IsPrivate,
    string? Project,
    string? Description,
    string? ForkPolicy);
