using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Api;

// The timeout moved from HttpClient.Timeout into BitbucketClient so the merge
// can wait longer than other calls. Program tells a timeout (exit 1) from
// Ctrl+C (exit 130) by an inner TimeoutException, so that shape must survive.
public class RequestTimeoutTests
{
    [Fact]
    public async Task A_request_that_outlasts_its_timeout_fails_as_a_timeout_not_a_cancellation()
    {
        var http = new FakeHttpMessageHandler { HangNext = true };
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());

        var act = () => client.PostForResponseAsync("x", new { }, TestContext.Current.CancellationToken,
            TimeSpan.FromMilliseconds(50));

        var thrown = await act.Should().ThrowAsync<TaskCanceledException>();
        thrown.Which.InnerException.Should().BeOfType<TimeoutException>();
    }

    [Fact]
    public async Task A_cancelled_request_still_reads_as_a_cancellation()
    {
        var http = new FakeHttpMessageHandler { HangNext = true };
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var act = () => client.PostForResponseAsync("x", new { }, cts.Token, TimeSpan.FromSeconds(30));

        var thrown = await act.Should().ThrowAsync<OperationCanceledException>();
        thrown.Which.InnerException.Should().NotBeOfType<TimeoutException>();
    }
}
