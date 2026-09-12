using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Composition;
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
        http.Enqueue(HttpStatusCode.OK, """{"page":1,"values":[],"size":0,"pagelen":0}""");
        var handler = new ListTestCasesHandler(Client(http), Creds());

        await handler.HandleAsync(
            new ListTestCasesRequest("ws", "myrepo", "{pl}", "{step}", 50), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pipelines/%7Bpl%7D/steps/%7Bstep%7D/test_reports/test_cases");
    }

    // A failed case as Bitbucket really sends it, trimmed. The field names are not the ones
    // the spec's silence invites: class_name, not classname, and an ISO 8601 duration.
    private const string FailedCase = """
        {"page":1,"size":1,"pagelen":1,"values":[{
          "uuid":"{80555a41-a1fe-4388-b15c-7ac308b80db3}",
          "fully_qualified_name":"Web.Tests.dll.Web.Tests.AuthTests.WrongPasswords_Lock(minutes: 30)",
          "name":"WrongPasswords_Lock(minutes: 30)",
          "class_name":"AuthTests",
          "suite_name":"Web.Tests.dll",
          "status":"FAILED",
          "duration":"PT0.9589264S",
          "reason":{"message":"Assert.Equal() Failure: Values differ","stack_trace":"at AuthTests.cs:line 63"}
        }]}
        """;

    [Fact]
    public async Task ListTestCases_reads_the_fields_Bitbucket_sends()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, FailedCase);
        var handler = new ListTestCasesHandler(Client(http), Creds());

        var result = (dynamic)await handler.HandleAsync(
            new ListTestCasesRequest("ws", "myrepo", "{pl}", "{step}", 50), TestContext.Current.CancellationToken);

        var json = JsonDocument.Parse(JsonSerializer.Serialize((object)result, JsonOptions.Compact)).RootElement;
        var testCase = json.GetProperty("test_cases")[0];
        testCase.GetProperty("uuid").GetString().Should().Be("{80555a41-a1fe-4388-b15c-7ac308b80db3}");
        testCase.GetProperty("classname").GetString().Should().Be("AuthTests");
        testCase.GetProperty("fully_qualified_name").GetString().Should().EndWith("WrongPasswords_Lock(minutes: 30)");
        testCase.GetProperty("status").GetString().Should().Be("FAILED");
        testCase.GetProperty("duration_ms").GetInt64().Should().Be(959);
        testCase.GetProperty("message").GetString().Should().Be("Assert.Equal() Failure: Values differ");
        json.GetProperty("note").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ListTestCases_explains_an_empty_page()
    {
        // What a step of 1,354 passing tests really answers.
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"page":1,"values":[],"size":0,"pagelen":0}""");
        var handler = new ListTestCasesHandler(Client(http), Creds());

        var result = (dynamic)await handler.HandleAsync(
            new ListTestCasesRequest("ws", "myrepo", "{pl}", "{step}", 50), TestContext.Current.CancellationToken);

        ((int)result.count).Should().Be(0);
        ((string)result.note).Should().Be(ListTestCasesHandler.OnlyFailedCasesNote);
    }

    [Theory]
    [InlineData("PT0.9589264S", 959L)]
    [InlineData("PT1M2.5S", 62500L)]
    [InlineData("PT0S", 0L)]
    [InlineData("not a duration", null)]
    [InlineData(null, null)]
    public void DurationMs_reads_ISO_8601(string? iso, long? expected) =>
        ListTestCasesHandler.DurationMs(iso).Should().Be(expected);
}
