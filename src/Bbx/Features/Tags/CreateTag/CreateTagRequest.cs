namespace Bbx.Features.Tags.CreateTag;

public sealed record CreateTagRequest(
    string? Workspace,
    string? Repository,
    string Name,
    string Target,
    string? Message);
