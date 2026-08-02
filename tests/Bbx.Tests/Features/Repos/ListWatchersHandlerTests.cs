using System.Net;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.ListWatchers;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Repos;

public class ListWatchersHandlerTests
{
    [Fact]
    public async Task HandleAsync_lists_from_watchers_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK,
            """{"values":[{"uuid":"{u1}","display_name":"Alice"},{"uuid":"{u2}","display_name":"Bob"}],"next":null}""");
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        var credentials = new CredentialManager(
            new InMemoryCredentialStore(new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" }));
        var handler = new ListWatchersHandler(client, credentials);

        var result = (dynamic)await handler.HandleAsync(
            new ListWatchersRequest("ws", "myrepo", 25), CancellationToken.None);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/watchers");
        ((int)result.count).Should().Be(2);
    }
}
