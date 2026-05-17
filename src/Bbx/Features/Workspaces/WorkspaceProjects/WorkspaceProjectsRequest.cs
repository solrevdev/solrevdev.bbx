namespace Bbx.Features.Workspaces.WorkspaceProjects;

public sealed record WorkspaceProjectsRequest(
    string? Workspace,
    string? View,
    string? Create,
    string? Key,
    string? Description,
    bool? IsPrivate,
    bool Delete,
    int Limit);
