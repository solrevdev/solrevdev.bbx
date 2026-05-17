namespace Bbx.Features.Tags.DeleteTag;

public sealed record DeleteTagRequest(string? Workspace, string? Repository, string Name);
