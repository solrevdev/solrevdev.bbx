using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Pipelines.ListPipelineReports;
using Bbx.Features.Pipelines.ListReportAnnotations;
using Bbx.Features.Pipelines.ListTestCases;
using Bbx.Features.Pipelines.ListTestReports;
using Bbx.Features.Pipelines.ViewPipelineReport;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Pipelines;

public class PipelineReportsHandlerTests
{
    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" }));

    [Fact]
    public async Task ListPipelineReports_hits_reports_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[{"uuid":"{r1}","title":"unit"}],"next":null}""");
        var handler = new ListPipelineReportsHandler(Client(http), Creds());

        var result = (dynamic)await handler.HandleAsync(
            new ListPipelineReportsRequest("ws", "myrepo", "abcdef", 25), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/commit/abcdef/reports");
        ((int)result.count).Should().Be(1);
    }

    [Fact]
    public async Task ViewPipelineReport_hits_keyed_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"uuid":"{r1}","title":"unit"}""");
        var handler = new ViewPipelineReportHandler(Client(http), Creds());

        await handler.HandleAsync(
            new ViewPipelineReportRequest("ws", "myrepo", "abc", "{r1}"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/commit/abc/reports/%7Br1%7D");
    }

    [Fact]
    public async Task ListReportAnnotations_hits_annotations_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");
        var handler = new ListReportAnnotationsHandler(Client(http), Creds());

        await handler.HandleAsync(
            new ListReportAnnotationsRequest("ws", "myrepo", "abc", "{r1}", 100), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/commit/abc/reports/%7Br1%7D/annotations");
    }

    [Fact]
    public async Task ListTestReports_nests_under_step()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, "{}");
        var handler = new ListTestReportsHandler(Client(http), Creds());

        await handler.HandleAsync(
            new ListTestReportsRequest("ws", "myrepo", "{pl}", "{step}"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pipelines/%7Bpl%7D/steps/%7Bstep%7D/test_reports");
    }

    [Fact]
    public async Task ListTestCases_nests_under_test_reports()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[{"name":"t1","status":"PASSED"}],"next":null}""");
        var handler = new ListTestCasesHandler(Client(http), Creds());

        var result = (dynamic)await handler.HandleAsync(
            new ListTestCasesRequest("ws", "myrepo", "{pl}", "{step}", 50), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pipelines/%7Bpl%7D/steps/%7Bstep%7D/test_reports/test_cases");
        ((int)result.count).Should().Be(1);
    }
}
