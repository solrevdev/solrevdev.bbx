using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Branches.AddBranchRestriction;
using Bbx.Features.Pipelines.CreatePipelineSchedule;
using Bbx.Features.Snippets.SnippetWatch;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Branches;

public class BranchRestrictionAndScheduleTests
{
    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    // Regression: an exact pattern was sent with branch_match_kind
    // "branching_model", which Bitbucket rejects with "pattern is only valid
    // when branch_match_kind is glob".
    [Theory]
    [InlineData("master")]
    [InlineData("release/*")]
    public async Task Branch_restriction_always_uses_glob_matching(string pattern)
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Created, """{"id":1,"kind":"push"}""");
        var handler = new AddBranchRestrictionHandler(Client(http), Creds());

        await handler.HandleAsync(
            new AddBranchRestrictionRequest("ws", "repo", "push", pattern), CancellationToken.None);

        var body = http.CallBodies.Single()!;
        body.Should().Contain("\"branch_match_kind\": \"glob\"");
        body.Should().Contain($"\"pattern\": \"{pattern}\"");
    }

    // Regression: the target had no "type" discriminator or selector, so
    // Bitbucket answered "An invalid field was found in the JSON payload".
    [Fact]
    public async Task Schedule_target_carries_its_type_and_a_default_selector()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"{s1}"}""");
        var handler = new CreatePipelineScheduleHandler(Client(http), Creds());

        await handler.HandleAsync(
            new CreatePipelineScheduleRequest("ws", "repo", "0 0 1 * * ? *", "master", null, true),
            CancellationToken.None);

        var body = http.CallBodies.Single()!;
        body.Should().Contain("\"type\": \"pipeline_ref_target\"");
        body.Should().Contain("\"ref_name\": \"master\"");
        body.Should().Contain("\"type\": \"branches\"");
        body.Should().Contain("\"pattern\": \"default\"");
    }

    [Fact]
    public async Task Schedule_uses_a_custom_selector_when_a_pattern_is_given()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Created, """{"uuid":"{s1}"}""");
        var handler = new CreatePipelineScheduleHandler(Client(http), Creds());

        await handler.HandleAsync(
            new CreatePipelineScheduleRequest("ws", "repo", "0 0 1 * * ? *", "master", "nightly", true),
            CancellationToken.None);

        var body = http.CallBodies.Single()!;
        body.Should().Contain("\"type\": \"custom\"").And.Contain("\"pattern\": \"nightly\"");
    }

    // Regression: PUT /watch answers 204 with no body, and PutAsync deserialized
    // the empty string, throwing "The input does not contain any JSON tokens".
    [Fact]
    public async Task Watching_a_snippet_tolerates_an_empty_204_body()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");
        var handler = new SnippetWatchHandler(Client(http), Creds());

        var result = await handler.HandleAsync(
            new SnippetWatchRequest("ws", "abc123", List: false, Unwatch: false), CancellationToken.None);

        result.Should().NotBeNull();
        http.Calls.Single().Method.Should().Be(HttpMethod.Put);
    }
}
