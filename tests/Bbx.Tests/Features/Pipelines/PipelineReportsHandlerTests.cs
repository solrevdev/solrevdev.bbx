using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Pipelines.ListPipelineReports;
using Bbx.Features.Pipelines.ListReportAnnotations;
using Bbx.Features.Pipelines.ListTestCases;
using Bbx.Features.Pipelines.ListTestReports;
using Bbx.Features.Pipelines.OidcConfig;
using Bbx.Features.Pipelines.OidcKeys;
using Bbx.Features.Pipelines.ViewPipelineReport;
using Bbx.Tests.TestKit;
using FluentAssertions;

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
            new ListPipelineReportsRequest("ws", "myrepo", "abcdef", 25), CancellationToken.None);

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
            new ViewPipelineReportRequest("ws", "myrepo", "abc", "{r1}"), CancellationToken.None);

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
            new ListReportAnnotationsRequest("ws", "myrepo", "abc", "{r1}", 100), CancellationToken.None);

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
            new ListTestReportsRequest("ws", "myrepo", "{pl}", "{step}"), CancellationToken.None);

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
            new ListTestCasesRequest("ws", "myrepo", "{pl}", "{step}", 50), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pipelines/%7Bpl%7D/steps/%7Bstep%7D/test_reports/test_cases");
        ((int)result.count).Should().Be(1);
    }

    [Fact]
    public async Task OidcConfig_and_Keys_hit_oidc_paths()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"issuer":"https://bitbucket.org/2.0"}""");
        var configHandler = new OidcConfigHandler(Client(http), Creds());
        await configHandler.HandleAsync(new OidcConfigRequest("ws", "myrepo"), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pipelines-config/identity/oidc/.well-known/openid-configuration");

        var http2 = new FakeHttpMessageHandler();
        http2.Enqueue(HttpStatusCode.OK, """{"keys":[]}""");
        var keysHandler = new OidcKeysHandler(Client(http2), Creds());
        await keysHandler.HandleAsync(new OidcKeysRequest("ws", "myrepo"), CancellationToken.None);

        http2.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/pipelines-config/identity/oidc/keys.json");
    }
}
