using System.Diagnostics;
using System.Text.Json;
using Bbx.Api;
using Bbx.Features.Common;

namespace Bbx.Features.PullRequests.PullRequestMergeStatus;

/// <summary>
/// A merge Bitbucket accepted with 202 and runs in the background, and the
/// polling of its task-status endpoint that both <c>pr merge</c> and
/// <c>pr merge-status</c> share.
/// </summary>
public static class MergeTask
{
    public const int DefaultTimeoutSeconds = 120;
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(3);

    private const string Marker = "/task-status/";

    /// <summary>
    /// Accept either the bare task ID or the whole task-status URL, so a caller
    /// can paste either value that <c>pr merge</c> printed.
    /// </summary>
    public static string IdFrom(string idOrUrl)
    {
        var value = idOrUrl.Trim();
        var at = value.IndexOf(Marker, StringComparison.OrdinalIgnoreCase);
        if (at < 0) return value;
        var id = value[(at + Marker.Length)..];
        var end = id.IndexOfAny(['/', '?', '#']);
        return Uri.UnescapeDataString(end < 0 ? id : id[..end]);
    }

    /// <summary>
    /// The task-status URL out of a 202: the Location header, which is where the
    /// spec puts it, or <c>links.self.href</c> in the body if Bitbucket sent one.
    /// </summary>
    public static string? StatusUrl(ApiResponse response)
    {
        if (response.Location is { } location) return location.AbsoluteUri;
        return response.Body.TryGetObject("links", out var links)
               && links.TryGetObject("self", out var self)
            ? self.GetStringOrNull("href")
            : null;
    }

    public static string Endpoint(string ws, string repo, int id, string taskId) =>
        $"repositories/{ws}/{repo}/pullrequests/{id}/merge/task-status/{Uri.EscapeDataString(taskId)}";

    /// <summary>
    /// What a queued merge prints: everything needed to follow it up, and a
    /// <c>status</c> nobody can mistake for a finished merge.
    /// </summary>
    public static object Queued(string ws, string repo, int id, string? taskId, string? statusUrl,
        string? taskStatus, int? waitedSeconds) => new
    {
        status = "queued",
        pull_request = id,
        task_id = taskId,
        task_status_url = statusUrl,
        task_status = taskStatus,
        waited_seconds = waitedSeconds,
        next = taskId is null
            ? $"bbx pr view {id} -w {ws} -r {repo}"
            : $"bbx pr merge-status {id} --task-id {taskId} -w {ws} -r {repo} --wait",
    };

    /// <summary>
    /// Poll until the task leaves PENDING or the timeout passes. Polls at least
    /// once, so a timeout of 0 is a single check.
    /// </summary>
    /// <remarks>
    /// SUCCESS yields the merged pull request, the same body a synchronous merge
    /// answers with, so a caller cannot tell the two paths apart. A timeout
    /// prints the queued record and exits <see cref="ExitCodes.Pending"/>,
    /// because during the 16 Nov 2026 incident queued merges were dropped
    /// silently and exit 0 would have hidden that. A failed task answers 400 with
    /// an error body, which the client turns into an exception and exit 1.
    /// </remarks>
    public static async Task<CommandOutcome> WaitAsync(BitbucketClient client, string ws, string repo, int id,
        string taskId, string? statusUrl, int timeoutSeconds, TimeSpan interval, CancellationToken ct)
    {
        var clock = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(Math.Max(0, timeoutSeconds));
        while (true)
        {
            var status = await client.GetAsync<JsonElement>(Endpoint(ws, repo, id, taskId), ct);
            var state = status.GetStringOrNull("task_status");

            if (string.Equals(state, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                return new CommandOutcome(
                    status.TryGetObject("merge_result", out var merged) ? merged : status);
            }

            if (!string.Equals(state, "PENDING", StringComparison.OrdinalIgnoreCase))
            {
                // A 200 that is neither PENDING nor SUCCESS: the spec shows a
                // failed task as an error object, so report whatever came back.
                var message = status.TryGetObject("error", out var error)
                    ? error.GetStringOrNull("message")
                    : null;
                return new CommandOutcome(status, 1,
                    $"Error: merge task {taskId} for PR #{id} did not succeed"
                    + (message is null ? $" (task_status: {state ?? "missing"})." : $": {message}"));
            }

            if (clock.Elapsed + interval > timeout)
            {
                var waited = (int)Math.Round(clock.Elapsed.TotalSeconds);
                return new CommandOutcome(
                    Queued(ws, repo, id, taskId, statusUrl, state, waited),
                    ExitCodes.Pending,
                    $"PR #{id} merge is still queued after {waited}s (task {taskId}). "
                    + "It may still land. Check again with `bbx pr merge-status`.");
            }

            await Task.Delay(interval, ct);
        }
    }
}
