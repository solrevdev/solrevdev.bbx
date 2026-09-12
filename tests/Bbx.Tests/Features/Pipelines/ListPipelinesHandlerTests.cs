using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Pipelines.ListPipelines;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Pipelines;

public class ListPipelinesHandlerTests
{
    private static ListPipelinesHandler Handler(FakeHttpMessageHandler http) =>
        new(new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(new BbxConfig
            { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" })));

    private static async Task<string> RequestedUrl(string? status, string? branch)
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await Handler(http).HandleAsync(
            new ListPipelinesRequest("ws", "myrepo", status, "-created_on", 25, branch),
            TestContext.Current.CancellationToken);

        return http.Calls.Single().RequestUri!.AbsoluteUri;
    }

    [Fact]
    public async Task No_filters_sends_sort_only() =>
        (await RequestedUrl(null, null))
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pipelines/?sort=-created_on");

    // Bitbucket ignores q= on this endpoint and answers the unfiltered list, which is what
    // --status and --branch silently did until 1.4.3.
    [Fact]
    public async Task Filters_are_plain_parameters_not_q()
    {
        var url = await RequestedUrl("failed", "feat/x");

        url.Should().Be(
            "https://api.bitbucket.org/2.0/repositories/ws/myrepo/pipelines/?sort=-created_on&status=FAILED&target.branch=feat%2Fx");
        url.Should().NotContain("q=");
    }

    // The printed result is SUCCESSFUL, but status=SUCCESSFUL matches nothing; PASSED does.
    [Theory]
    [InlineData("SUCCESSFUL")]
    [InlineData("successful")]
    [InlineData("PASSED")]
    public async Task A_pass_is_asked_for_as_PASSED(string status) =>
        (await RequestedUrl(status, null)).Should().EndWith("&status=PASSED");
}
