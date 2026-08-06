namespace Bbx.Features.Workspaces.Projects.UpdateProject;

/// <summary>
/// A partial update. Only non-null fields reach the wire.
/// </summary>
public sealed record UpdateProjectRequest(
    string? Workspace,
    string ProjectKey,
    string? Name,
    string? Description,
    bool? IsPrivate,
    string? NewKey);
