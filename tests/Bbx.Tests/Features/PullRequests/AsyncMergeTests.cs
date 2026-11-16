using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;
using Bbx.Features.PullRequests.MergePullRequest;
using Bbx.Features.PullRequests.PullRequestMergeStatus;
using Bbx.Tests.TestKit;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Tests.Features.PullRequests;

// Bitbucket answers a merge with 202 and a task-status link in the Location
// header when it queues the merge. bbx 1.5.0 printed "" and exited 0, so the
// task ID was lost and two queued merges were dropped without anyone knowing
// (16 Nov 2026). Every response here is faked; nothing is merged.
[Collection("Console")]
public sealed class AsyncMergeTests : IDisposable
{
    private const string TaskUrl =
        "https://api.bitbucket.org/2.0/repositories/ws/repo/pullrequests/21/merge/task-status/task-abc";

    private const string Merged = """{"id":21,"state":"MERGED","merge_commit":{"hash":"7339c57"}}""";

    private readonly IServiceProvider? _originalServices = Program.Services;

    [Fact]
    public async Task A_queued_merge_returns_the_task_at_once_with_no_wait()
    {
        var http = new FakeHttpMessageHandler();
        EnqueueAccepted(http);

        var outcome = await Merge(http, wait: false);

        outcome.ExitCode.Should().Be(0);
        var json = Json(outcome.Output);
        json.GetProperty("status").GetString().Should().Be("queued");
        json.GetProperty("task_id").GetString().Should().Be("task-abc");
        json.GetProperty("task_status_url").GetString().Should().Be(TaskUrl);
        json.GetProperty("next").GetString().Should()
            .Be("bbx pr merge-status 21 --task-id task-abc -w ws -r repo --wait");
        http.Calls.Should().HaveCount(1);
    }

    // Without async=true Bitbucket holds the POST open while it merges, and on
    // 16 Nov 2026 that outlasted the 30s client timeout, leaving the outcome
    // unknown.
    [Fact]
    public async Task The_merge_is_requested_asynchronously_so_a_slow_bitbucket_cannot_time_it_out()
    {
        var http = new FakeHttpMessageHandler();
        EnqueueAccepted(http);

        await Merge(http, wait: false);

        http.Calls.Single().RequestUri!.Query.Should().Be("?async=true");
    }

    [Fact]
    public async Task A_queued_merge_is_polled_until_it_succeeds_and_returns_the_merged_pull_request()
    {
        var http = new FakeHttpMessageHandler();
        EnqueueAccepted(http);
        http.Enqueue(HttpStatusCode.OK, """{"task_status":"PENDING"}""");
        http.Enqueue(HttpStatusCode.OK, $$"""{"task_status":"SUCCESS","merge_result":{{Merged}}}""");

        var outcome = await Merge(http, wait: true, timeoutSeconds: 60);

        outcome.ExitCode.Should().Be(0);
        Json(outcome.Output).GetProperty("state").GetString().Should().Be("MERGED");
        http.Calls.Skip(1).Should().AllSatisfy(c =>
            c.RequestUri!.AbsoluteUri.Should().Be(TaskUrl));
    }

    [Fact]
    public async Task A_merge_still_queued_at_the_timeout_prints_the_task_and_exits_pending()
    {
        var http = new FakeHttpMessageHandler();
        EnqueueAccepted(http);
        http.Enqueue(HttpStatusCode.OK, """{"task_status":"PENDING"}""");

        var outcome = await Merge(http, wait: true, timeoutSeconds: 0);

        outcome.ExitCode.Should().Be(ExitCodes.Pending);
        outcome.Message.Should().Contain("task-abc");
        var json = Json(outcome.Output);
        json.GetProperty("status").GetString().Should().Be("queued");
        json.GetProperty("task_status").GetString().Should().Be("PENDING");
    }

    // The spec shows a failed task as an error body. A 400 becomes an exception
    // and exit 1, as every other API failure does.
    [Fact]
    public async Task A_queued_merge_that_fails_is_reported_as_a_failure()
    {
        var http = new FakeHttpMessageHandler();
        EnqueueAccepted(http);
        http.Enqueue(HttpStatusCode.BadRequest, """{"type":"error","error":{"message":"Merge failed"}}""");

        var act = () => Merge(http, wait: true, timeoutSeconds: 60);

        (await act.Should().ThrowAsync<HttpRequestException>()).WithMessage("*Merge failed*");
    }

    // Live on 16 Nov 2026: after two minutes of PENDING one poll hung past the
    // 30s request timeout and the whole wait aborted with exit 1.
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task A_poll_that_fails_transiently_does_not_end_the_wait(HttpStatusCode failure)
    {
        var http = new FakeHttpMessageHandler();
        EnqueueAccepted(http);
        http.Enqueue(HttpStatusCode.OK, """{"task_status":"PENDING"}""");
        http.Enqueue(failure, """{"error":{"message":"slow"}}""");
        http.Enqueue(HttpStatusCode.OK, $$"""{"task_status":"SUCCESS","merge_result":{{Merged}}}""");

        var outcome = await Merge(http, wait: true, timeoutSeconds: 60);

        outcome.ExitCode.Should().Be(0);
        Json(outcome.Output).GetProperty("state").GetString().Should().Be("MERGED");
    }

    [Fact]
    public async Task A_poll_that_hangs_does_not_end_the_wait()
    {
        var http = new FakeHttpMessageHandler();
        EnqueueAccepted(http);
        http.EnqueueResponder(_ => throw new TaskCanceledException("hung", new TimeoutException()));
        http.Enqueue(HttpStatusCode.OK, $$"""{"task_status":"SUCCESS","merge_result":{{Merged}}}""");

        var outcome = await Merge(http, wait: true, timeoutSeconds: 60);

        outcome.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task A_poll_that_keeps_failing_still_ends_with_the_task_and_exit_75()
    {
        var http = new FakeHttpMessageHandler();
        EnqueueAccepted(http);
        http.Enqueue(HttpStatusCode.ServiceUnavailable, """{"error":{"message":"down"}}""");

        var outcome = await Merge(http, wait: true, timeoutSeconds: 0);

        outcome.ExitCode.Should().Be(ExitCodes.Pending);
        var json = Json(outcome.Output);
        json.GetProperty("task_id").GetString().Should().Be("task-abc");
        json.GetProperty("last_poll_error").GetString().Should().Contain("down");
    }

    [Fact]
    public async Task A_task_status_that_is_neither_pending_nor_success_exits_one()
    {
        var http = new FakeHttpMessageHandler();
        EnqueueAccepted(http);
        http.Enqueue(HttpStatusCode.OK, """{"task_status":"FAILED"}""");

        var outcome = await Merge(http, wait: true, timeoutSeconds: 60);

        outcome.ExitCode.Should().Be(1);
        outcome.Message.Should().Contain("FAILED");
    }

    // The spec says Location; the 202 seen during the incident was described
    // with links.self. Either one has to yield the task.
    [Fact]
    public async Task The_task_link_is_read_from_the_body_when_there_is_no_location_header()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Accepted, "{\"links\":{\"self\":{\"href\":\"" + TaskUrl + "\"}}}");

        var outcome = await Merge(http, wait: false);

        Json(outcome.Output).GetProperty("task_id").GetString().Should().Be("task-abc");
    }

    [Fact]
    public async Task A_202_with_no_task_link_exits_pending_rather_than_claiming_success()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Accepted, "");

        var outcome = await Merge(http, wait: true, timeoutSeconds: 60);

        outcome.ExitCode.Should().Be(ExitCodes.Pending);
        Json(outcome.Output).GetProperty("status").GetString().Should().Be("queued");
    }

    [Fact]
    public async Task A_merge_post_that_times_out_says_the_merge_may_still_be_queued()
    {
        var http = new FakeHttpMessageHandler();
        http.EnqueueResponder(_ => throw new TaskCanceledException(
            "timed out", new TimeoutException()));

        var act = () => Merge(http, wait: true);

        (await act.Should().ThrowAsync<BbxUserException>()).WithMessage("*may still have been queued*");
    }

    [Fact]
    public async Task A_synchronous_merge_still_returns_the_pull_request()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Merged);

        var outcome = await Merge(http, wait: true);

        outcome.ExitCode.Should().Be(0);
        Json(outcome.Output).GetProperty("id").GetInt32().Should().Be(21);
    }

    [Theory]
    [InlineData("task-abc")]
    [InlineData(TaskUrl)]
    [InlineData(TaskUrl + "?x=1")]
    public void Merge_status_takes_the_task_id_or_the_url_merge_printed(string given)
    {
        MergeTask.IdFrom(given).Should().Be("task-abc");
    }

    [Fact]
    public async Task Merge_status_can_wait_for_the_task()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"task_status":"PENDING"}""");
        http.Enqueue(HttpStatusCode.OK, $$"""{"task_status":"SUCCESS","merge_result":{{Merged}}}""");

        var outcome = await new PullRequestMergeStatusHandler(Client(http), Creds()).HandleAsync(
            new PullRequestMergeStatusRequest("ws", "repo", 21, TaskUrl, Wait: true, TimeoutSeconds: 60,
                PollInterval: TimeSpan.Zero),
            TestContext.Current.CancellationToken);

        Json(outcome.Output).GetProperty("state").GetString().Should().Be("MERGED");
        http.Calls.Should().AllSatisfy(c => c.RequestUri!.AbsoluteUri.Should().Be(TaskUrl));
    }

    // End to end through Program: the JSON reaches stdout and the exit code is
    // not zero, which is the pair a script needs.
    [Fact]
    public async Task The_cli_prints_the_task_and_exits_75_when_the_merge_is_still_queued()
    {
        var http = new FakeHttpMessageHandler();
        EnqueueAccepted(http);
        http.Enqueue(HttpStatusCode.OK, """{"task_status":"PENDING"}""");

        int exitCode = 0;
        var (stdout, stderr) = await CaptureConsole.RunAsync(async () =>
            exitCode = await Program.RunAsync(
                ["pr", "merge", "21", "-w", "ws", "-r", "repo", "--strategy", "merge_commit", "--yes",
                 "--timeout", "0"],
                Provider(http)));

        exitCode.Should().Be(ExitCodes.Pending);
        stdout.Should().Contain("\"status\": \"queued\"").And.Contain("\"task_id\": \"task-abc\"");
        stdout.Trim().Should().NotBe("\"\"");
        stderr.Should().Contain("still queued");
    }

    [Fact]
    public async Task The_cli_exits_zero_with_no_wait()
    {
        var http = new FakeHttpMessageHandler();
        EnqueueAccepted(http);

        int exitCode = -1;
        var (stdout, _) = await CaptureConsole.RunAsync(async () =>
            exitCode = await Program.RunAsync(
                ["pr", "merge", "21", "-w", "ws", "-r", "repo", "--strategy", "merge_commit", "--yes",
                 "--no-wait"],
                Provider(http)));

        exitCode.Should().Be(0);
        stdout.Should().Contain("\"task_id\": \"task-abc\"");
    }

    private static void EnqueueAccepted(FakeHttpMessageHandler http) =>
        http.EnqueueResponder(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("") };
            response.Headers.Location = new Uri(TaskUrl);
            return response;
        });

    private static Task<CommandOutcome> Merge(FakeHttpMessageHandler http, bool wait, int timeoutSeconds = 60) =>
        new MergePullRequestHandler(Client(http), Creds()).HandleAsync(
            new MergePullRequestRequest("ws", "repo", 21, "merge_commit", null, false, wait, timeoutSeconds,
                TimeSpan.Zero),
            TestContext.Current.CancellationToken);

    private static JsonElement Json(object? value) => JsonSerializer.SerializeToElement(value);

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static IServiceProvider Provider(FakeHttpMessageHandler http)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Creds());
        services.AddSingleton(Client(http));
        services.AddTransient<MergePullRequestHandler>();
        return services.BuildServiceProvider();
    }

    public void Dispose() => Program.Services = _originalServices!;
}
