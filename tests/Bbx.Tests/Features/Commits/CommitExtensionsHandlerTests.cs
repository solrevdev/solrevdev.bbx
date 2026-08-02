using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Commits.ApproveCommit;
using Bbx.Features.Commits.CommitDiffstat;
using Bbx.Features.Commits.FileHistory;
using Bbx.Features.Commits.MergeBase;
using Bbx.Features.Commits.UnapproveCommit;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Commits;

public class CommitExtensionsHandlerTests
{
    [Fact]
    public async Task FileHistory_composes_url_with_hash_and_escaped_path()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"values":[],"next":null}""");
        var handler = new FileHistoryHandler(
            new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(
                new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" })));

        await handler.HandleAsync(
            new FileHistoryRequest("ws", "myrepo", "abcdef", "src/file with space.cs", 25),
            TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/filehistory/abcdef/src/file%20with%20space.cs");
    }

    [Fact]
    public async Task FileHistory_throws_when_path_missing()
    {
        var http = new FakeHttpMessageHandler();
        var handler = new FileHistoryHandler(
            new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(
                new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" })));

        var act = async () => await handler.HandleAsync(
            new FileHistoryRequest("ws", "myrepo", "abc", "", 25), TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*<path>*");
    }

    [Fact]
    public async Task MergeBase_escapes_spec_in_url()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"hash":"abcdef","date":"2026-01-01"}""");
        var handler = new MergeBaseHandler(
            new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(
                new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" })));

        await handler.HandleAsync(new MergeBaseRequest("ws", "myrepo", "feature..main"), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/merge-base/feature..main");
    }

    [Fact]
    public async Task ApproveCommit_posts_to_approve_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, "");
        var handler = new ApproveCommitHandler(
            new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(
                new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" })));

        await handler.HandleAsync(new ApproveCommitRequest("ws", "myrepo", "abcdef"), TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Post);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/commit/abcdef/approve");
    }

    [Fact]
    public async Task UnapproveCommit_deletes_approve_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.NoContent, "");
        var handler = new UnapproveCommitHandler(
            new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(
                new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" })));

        await handler.HandleAsync(new UnapproveCommitRequest("ws", "myrepo", "abcdef"), TestContext.Current.CancellationToken);

        http.Calls.Single().Method.Should().Be(HttpMethod.Delete);
        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/commit/abcdef/approve");
    }

    [Fact]
    public async Task Diffstat_lists_from_diffstat_endpoint()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """
            {"values":[
                {"status":"modified","lines_added":5,"lines_removed":2,"new":{"path":"a.cs"},"old":{"path":"a.cs"}}
            ],"next":null}
            """);
        var handler = new CommitDiffstatHandler(
            new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider()),
            new CredentialManager(new InMemoryCredentialStore(
                new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" })));

        var result = (dynamic)await handler.HandleAsync(
            new CommitDiffstatRequest("ws", "myrepo", "feature..main", 100), TestContext.Current.CancellationToken);

        http.Calls.Single().RequestUri!.AbsoluteUri
            .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/diffstat/feature..main");
        ((int)result.count).Should().Be(1);
    }
}
