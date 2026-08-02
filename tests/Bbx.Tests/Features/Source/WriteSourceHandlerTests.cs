using System.Net;
using System.Text;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Source.WriteSource;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Source;

public class WriteSourceHandlerTests
{
    [Fact]
    public async Task HandleAsync_posts_multipart_with_message_branch_and_files()
    {
        var local = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(local, "hello world", Encoding.UTF8, TestContext.Current.CancellationToken);

            var handler = BuildHandler(out var http,
                seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });
            http.Enqueue(HttpStatusCode.Created, "");

            await handler.HandleAsync(
                new WriteSourceRequest("ws", "myrepo", "main", "add file",
                    new[] { $"{local}=path/to/file.txt" }, "Jane <jane@example.com>"),
                TestContext.Current.CancellationToken);

            var call = http.Calls.Single();
            call.Method.Should().Be(HttpMethod.Post);
            call.RequestUri!.AbsoluteUri
                .Should().Be("https://api.bitbucket.org/2.0/repositories/ws/myrepo/src");

            var body = http.CallBodies.Single()!;
            body.Should().Contain("name=message").And.Contain("add file");
            body.Should().Contain("name=branch").And.Contain("main");
            body.Should().Contain("name=author").And.Contain("Jane <jane@example.com>");
            // Form-part name is the repo-relative path; HttpClient quotes names
            // containing slashes.
            body.Should().Contain("name=\"path/to/file.txt\"");
            body.Should().Contain("hello world");
        }
        finally
        {
            File.Delete(local);
        }
    }

    [Fact]
    public async Task HandleAsync_throws_when_no_files_provided()
    {
        var handler = BuildHandler(out _,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });

        var act = async () => await handler.HandleAsync(
            new WriteSourceRequest("ws", "myrepo", "main", "msg", Array.Empty<string>(), null),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*--file*");
    }

    [Fact]
    public async Task HandleAsync_throws_when_local_file_missing()
    {
        var handler = BuildHandler(out _,
            seed: new BbxConfig { DefaultWorkspace = "ws", Username = "u", ApiToken = "t" });

        var act = async () => await handler.HandleAsync(
            new WriteSourceRequest("ws", "myrepo", "main", "msg",
                new[] { "/this/does/not/exist=foo.txt" }, null),
            TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("Error: File not found:*");
    }

    private static WriteSourceHandler BuildHandler(out FakeHttpMessageHandler http, BbxConfig? seed)
    {
        http = new FakeHttpMessageHandler();
        var store = seed is null ? new InMemoryCredentialStore() : new InMemoryCredentialStore(seed);
        var credentials = new CredentialManager(store);
        var client = new BitbucketClient(TestHttpClientFactory.Create(http), new NullAuthProvider());
        return new WriteSourceHandler(client, credentials);
    }
}
