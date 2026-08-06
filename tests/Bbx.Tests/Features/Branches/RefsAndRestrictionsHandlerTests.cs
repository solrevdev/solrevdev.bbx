using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Branches.ListRefs;
using Bbx.Features.Branches.UpdateBranchRestriction;
using Bbx.Features.Branches.ViewBranchRestriction;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Branches;

public class RefsAndRestrictionsHandlerTests
{
    private static BitbucketClient Client(FakeHttpMessageHandler http) =>
        new(TestHttpClientFactory.Create(http), new NullAuthProvider());

    private static CredentialManager Creds() =>
        new(new InMemoryCredentialStore(new BbxConfig
        { Username = "jane@example.com", ApiToken = "t", DefaultWorkspace = "ws" }));

    private static JsonElement Body(FakeHttpMessageHandler http)
        => JsonDocument.Parse(http.CallBodies[0]!).RootElement;

    [Fact]
    public async Task Restriction_view_gets_the_single_restriction_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":42,"kind":"push"}""");

        await new ViewBranchRestrictionHandler(Client(http), Creds()).HandleAsync(
            new ViewBranchRestrictionRequest("ws", "repo", 42), TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Get);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/branch-restrictions/42");
    }

    [Fact]
    public async Task Restriction_update_puts_only_the_fields_it_was_given()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":42}""");

        await new UpdateBranchRestrictionHandler(Client(http), Creds()).HandleAsync(
            new UpdateBranchRestrictionRequest("ws", "repo", 42, "release/*", null, null, null),
            TestContext.Current.CancellationToken);

        var call = http.Calls.Single();
        call.Method.Should().Be(HttpMethod.Put);
        call.RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/branch-restrictions/42");
        var body = Body(http);
        body.GetProperty("pattern").GetString().Should().Be("release/*");
        body.TryGetProperty("value", out _).Should().BeFalse();
        body.TryGetProperty("users", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Restriction_update_wraps_users_and_groups()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":42}""");

        await new UpdateBranchRestrictionHandler(Client(http), Creds()).HandleAsync(
            new UpdateBranchRestrictionRequest("ws", "repo", 42, null, 2, ["{uuid-1}"], ["admins"]),
            TestContext.Current.CancellationToken);

        var body = Body(http);
        body.GetProperty("value").GetInt32().Should().Be(2);
        body.GetProperty("users").EnumerateArray().Single().GetProperty("uuid").GetString()
            .Should().Be("{uuid-1}");
        body.GetProperty("groups").EnumerateArray().Single().GetProperty("slug").GetString()
            .Should().Be("admins");
    }

    // An absent array option parses to an empty array, so keying off null would
    // send an empty exemption list and strip the exemptions off the rule.
    [Fact]
    public async Task An_empty_users_array_is_not_sent()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"id":42}""");

        await new UpdateBranchRestrictionHandler(Client(http), Creds()).HandleAsync(
            new UpdateBranchRestrictionRequest("ws", "repo", 42, "main", null, [], []),
            TestContext.Current.CancellationToken);

        var body = Body(http);
        body.TryGetProperty("users", out _).Should().BeFalse();
        body.TryGetProperty("groups", out _).Should().BeFalse();
    }

    [Fact]
    public async Task An_empty_array_does_not_count_as_something_to_update()
    {
        var http = new FakeHttpMessageHandler();

        var act = async () => await new UpdateBranchRestrictionHandler(Client(http), Creds()).HandleAsync(
            new UpdateBranchRestrictionRequest("ws", "repo", 42, null, null, [], []),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>()
            .WithMessage(UpdateBranchRestrictionHandler.NothingToUpdate);
        http.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Refs_lists_branches_and_tags_together()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"type":"branch","name":"main","target":{"hash":"abc"}},
                {"type":"tag","name":"v1.0","target":{"hash":"def"}}
            ],"next":null}
            """);

        var result = (dynamic)await new ListRefsHandler(Client(http), Creds()).HandleAsync(
            new ListRefsRequest("ws", "repo", null, 100), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsolutePath.Should().Be("/2.0/repositories/ws/repo/refs");
        ((int)result.count).Should().Be(2);
    }

    [Fact]
    public async Task Refs_passes_the_query_through_as_q()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await new ListRefsHandler(Client(http), Creds()).HandleAsync(
            new ListRefsRequest("ws", "repo", "name ~ \"release\"", 100),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.Query.Should().StartWith("?q=");
    }

    [Fact]
    public async Task Refs_stops_at_the_limit()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[{"type":"branch","name":"a"},{"type":"branch","name":"b"}],"next":null}
            """);

        var result = (dynamic)await new ListRefsHandler(Client(http), Creds()).HandleAsync(
            new ListRefsRequest("ws", "repo", null, 1), TestContext.Current.CancellationToken);

        ((int)result.count).Should().Be(1);
    }
}
