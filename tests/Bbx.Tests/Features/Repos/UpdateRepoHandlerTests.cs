using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.UpdateRepo;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Repos;

public class UpdateRepoHandlerTests
{
    private const string Ok = """{"slug":"repo","name":"Repo"}""";

    private static UpdateRepoHandler Handler(FakeHttpMessageHandler http) =>
        new(new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(new BbxConfig
            { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" })));

    private static UpdateRepoRequest Request(
        string? name = null, string? description = null, bool? isPrivate = null,
        string? forkPolicy = null, string? language = null, string? website = null,
        string? project = null, string? mainBranch = null, bool? hasIssues = null,
        bool? hasWiki = null, string repository = "repo")
        => new("ws", repository, name, description, isPrivate, forkPolicy, language, website,
            project, mainBranch, hasIssues, hasWiki);

    private static JsonElement Body(FakeHttpMessageHandler http)
        => JsonDocument.Parse(http.CallBodies[0]!).RootElement;

    [Fact]
    public async Task Update_puts_to_the_repository_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(Request(name: "New"), TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Put);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo");
    }

    // This PUT merges the same way the pull request one does, so sending an
    // untouched field would overwrite a setting the caller never mentioned.
    [Fact]
    public async Task Update_sends_only_the_fields_the_caller_set()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(Request(description: "d"), TestContext.Current.CancellationToken);

        var body = Body(http);
        body.GetProperty("description").GetString().Should().Be("d");
        foreach (var absent in new[] { "name", "is_private", "fork_policy", "language", "website", "project", "mainbranch", "has_issues", "has_wiki" })
        {
            body.TryGetProperty(absent, out _).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Update_nests_project_and_main_branch()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(
            Request(project: "PROJ", mainBranch: "trunk"), TestContext.Current.CancellationToken);

        var body = Body(http);
        body.GetProperty("project").GetProperty("key").GetString().Should().Be("PROJ");
        body.GetProperty("mainbranch").GetProperty("name").GetString().Should().Be("trunk");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_sends_the_boolean_settings_it_was_given(bool value)
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(
            Request(isPrivate: value, hasIssues: value, hasWiki: value),
            TestContext.Current.CancellationToken);

        var body = Body(http);
        body.GetProperty("is_private").GetBoolean().Should().Be(value);
        body.GetProperty("has_issues").GetBoolean().Should().Be(value);
        body.GetProperty("has_wiki").GetBoolean().Should().Be(value);
    }

    // An empty PUT is accepted and changes nothing, which would report success
    // for a command that did no work.
    [Fact]
    public async Task Update_with_no_fields_fails_before_calling_the_api()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await Handler(http).HandleAsync(Request(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>().WithMessage(UpdateRepoHandler.NothingToUpdate);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_accepts_a_workspace_slash_repo_path()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, Ok);

        await Handler(http).HandleAsync(
            Request(name: "New", repository: "other/thing"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/other/thing");
    }
}
