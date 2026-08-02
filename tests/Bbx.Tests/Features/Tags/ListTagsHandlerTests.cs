using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Tags.ListTags;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Tags;

public class ListTagsHandlerTests
{
    [Fact]
    public async Task HandleAsync_composes_url_with_sort_and_query()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");

        await handler.HandleAsync(
            new ListTagsRequest("ws", "myrepo", 10, "-name", "name~\"v1\""),
            TestContext.Current.CancellationToken);

        var uri = http.Calls.Single().RequestUri!;
        uri.AbsolutePath.Should().Be("/2.0/repositories/ws/myrepo/refs/tags");
        uri.Query.Should().Contain("sort=-name").And.Contain("q=name~%22v1%22");
    }

    [Fact]
    public async Task HandleAsync_throws_when_workspace_or_repo_missing()
    {
        var handler = BuildHandler(out _, seed: null);

        var act = async () => await handler.HandleAsync(
            new ListTagsRequest(null, null, 25, null, null), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BbxUserException>();
    }

    [Fact]
    public async Task HandleAsync_projects_summary_fields_and_stops_at_limit()
    {
        var handler = BuildHandler(out var http,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"name":"v1.0.0","target":{"hash":"abcdef123456"}},
                {"name":"v1.0.1","target":{"hash":"123456abcdef"}},
                {"name":"v1.1.0","target":{"hash":"deadbeefcafe"}}
            ],"next":null}
            """);

        var result = (dynamic)await handler.HandleAsync(
            new ListTagsRequest("ws", "myrepo", 2, null, null), TestContext.Current.CancellationToken);

        ((int)result.count).Should().Be(2);
    }

    private static ListTagsHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new ListTagsHandler(client, credentials);
    }
}
