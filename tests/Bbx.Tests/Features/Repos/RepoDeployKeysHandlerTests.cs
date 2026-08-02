using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Repos.DeployKeys.AddRepoDeployKey;
using Bbx.Features.Repos.DeployKeys.DeleteRepoDeployKey;
using Bbx.Features.Repos.DeployKeys.ListRepoDeployKeys;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Repos;

public class RepoDeployKeysHandlerTests
{
    [Fact]
    public async Task List_hits_deploy_keys_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK,
            """{"values":[{"id":1,"label":"prod","key":"ssh-ed25519 AAAA..."}],"next":null}""");
        var handler = new ListRepoDeployKeysHandler(
            new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(
                new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" })));

        var result = (dynamic)await handler.HandleAsync(
            new ListRepoDeployKeysRequest("ws", "myrepo", 25), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/deploy-keys");
        ((int)result.count).Should().Be(1);
    }

    [Fact]
    public async Task Add_posts_key_and_optional_label()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Created, """{"id":1,"label":"prod","key":"ssh-ed25519 AAAA"}""");
        var handler = new AddRepoDeployKeyHandler(
            new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(
                new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" })));

        await handler.HandleAsync(
            new AddRepoDeployKeyRequest("ws", "myrepo", "ssh-ed25519 AAAA", "prod"),
            TestContext.Current.CancellationToken);

        var body = JsonDocument.Parse(http.CallBodies.Single()!);
        body.RootElement.GetProperty("key").GetString().Should().Be("ssh-ed25519 AAAA");
        body.RootElement.GetProperty("label").GetString().Should().Be("prod");
    }

    [Fact]
    public async Task Delete_deletes_keyed_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");
        var handler = new DeleteRepoDeployKeyHandler(
            new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(
                new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" })));

        await handler.HandleAsync(new DeleteRepoDeployKeyRequest("ws", "myrepo", 7), TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Delete);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/deploy-keys/7");
    }
}
