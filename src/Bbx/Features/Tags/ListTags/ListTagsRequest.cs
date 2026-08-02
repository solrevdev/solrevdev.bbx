namespace Bbx.Features.Tags.ListTags;

public sealed record ListTagsRequest(string? Workspace, string? Repository, int Limit, string? Sort, string? Query);
