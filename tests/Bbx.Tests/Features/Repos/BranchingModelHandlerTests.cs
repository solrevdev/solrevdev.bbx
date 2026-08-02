using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.BranchingModel.UpdateBranchingModelSettings;
using Bbx.Features.Repos.BranchingModel.ViewBranchingModel;
using Bbx.Features.Repos.BranchingModel.ViewBranchingModelSettings;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Repos;

public class BranchingModelHandlerTests
{
    [Fact]
    public async Task ViewBranchingModel_hits_branching_model_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"development":{"name":"main"}}""");
        var handler = new ViewBranchingModelHandler(
            new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(
                new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" })));

        await handler.HandleAsync(new ViewBranchingModelRequest("ws", "myrepo"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/branching-model");
    }

    [Fact]
    public async Task ViewBranchingModelSettings_hits_settings_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"branch_types":[]}""");
        var handler = new ViewBranchingModelSettingsHandler(
            new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(
                new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" })));

        await handler.HandleAsync(new ViewBranchingModelSettingsRequest("ws", "myrepo"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/branching-model/settings");
    }

    [Fact]
    public async Task UpdateBranchingModelSettings_puts_parsed_json_payload()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"development":{"name":"main"}}""");
        var handler = new UpdateBranchingModelSettingsHandler(
            new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(
                new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" })));

        await handler.HandleAsync(
            new UpdateBranchingModelSettingsRequest("ws", "myrepo",
                """{"development":{"name":"main","use_mainbranch":true}}"""),
            TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Put);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/branching-model/settings");
        var body = JsonDocument.Parse(http.CallBodies.Single()!);
        body.RootElement.GetProperty("development").GetProperty("name").GetString().Should().Be("main");
    }

    [Fact]
    public async Task UpdateBranchingModelSettings_throws_on_invalid_json()
    {
        var http = new FakeHttpMessageHandler();
        var handler = new UpdateBranchingModelSettingsHandler(
            new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(
                new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" })));

        var act = async () => await handler.HandleAsync(
            new UpdateBranchingModelSettingsRequest("ws", "myrepo", "{not json"),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*not valid JSON*");
    }
}
