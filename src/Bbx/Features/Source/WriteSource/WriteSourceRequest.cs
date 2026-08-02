namespace Bbx.Features.Source.WriteSource;

public sealed record WriteSourceRequest(
    string? Workspace,
    string? Repository,
    string Branch,
    string Message,
    string[] Files,
    string? Author);
