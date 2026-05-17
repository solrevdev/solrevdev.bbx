namespace Bbx.Features.Pipelines.TriggerPipeline;

public sealed record TriggerPipelineRequest(
    string? Workspace,
    string? Repository,
    string Branch,
    string? Commit,
    string? Pattern,
    string? PullRequestId,
    string[] Variables);
