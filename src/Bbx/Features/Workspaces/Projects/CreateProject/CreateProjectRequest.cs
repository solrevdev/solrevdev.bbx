namespace Bbx.Features.Workspaces.Projects.CreateProject;

public sealed record CreateProjectRequest(
    string? Workspace,
    string ProjectKey,
    string Name,
    string? Description,
    bool IsPrivate);
