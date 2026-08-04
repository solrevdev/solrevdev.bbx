using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Api;

public class BitbucketClientTests
{
    [Fact]
    public async Task GetAsync_strips_leading_slash_and_resolves_against_base_address()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, """{"display_name":"Jane"}""");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        await client.GetAsync<JsonElement>("/user", TestContext.Current.CancellationToken);

        handler.Calls.Should().ContainSingle();
        handler.Calls[0].RequestUri.Should().Be(new Uri("https://api.bitbucket.org/2.0/user"));
    }

    [Fact]
    public async Task GetAsync_keeps_relative_endpoint_when_no_leading_slash()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{}");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        await client.GetAsync<JsonElement>("repositories/ws/repo", TestContext.Current.CancellationToken);

        handler.Calls[0].RequestUri.Should().Be(new Uri("https://api.bitbucket.org/2.0/repositories/ws/repo"));
    }

    [Fact]
    public async Task BasicAuthProvider_applies_authorization_header()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{}");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new BasicAuthProvider("jane@example.com", "ATATT3xFfGF0"));

        await client.GetAsync<JsonElement>("user", TestContext.Current.CancellationToken);

        var auth = handler.Calls[0].Headers.Authorization;
        auth.Should().NotBeNull();
        auth!.Scheme.Should().Be("Basic");
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(auth.Parameter!));
        decoded.Should().Be("jane@example.com:ATATT3xFfGF0");
    }

    [Fact]
    public async Task NullAuthProvider_does_not_apply_authorization_header()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{}");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        await client.GetAsync<JsonElement>("user", TestContext.Current.CancellationToken);

        handler.Calls[0].Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task GetPaginatedAsync_follows_next_link_until_exhausted()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK,
            """{"values":[{"name":"a"},{"name":"b"}],"next":"https://api.bitbucket.org/2.0/repositories/ws?page=2"}""");
        handler.Enqueue(HttpStatusCode.OK, """{"values":[{"name":"c"}]}""");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        var names = new List<string?>();
        await foreach (var element in client.GetPaginatedAsync<JsonElement>("repositories/ws", TestContext.Current.CancellationToken))
        {
            names.Add(element.GetProperty("name").GetString());
        }

        names.Should().Equal("a", "b", "c");
        handler.Calls.Should().HaveCount(2);
        handler.Calls[0].RequestUri.Should().Be(new Uri("https://api.bitbucket.org/2.0/repositories/ws"));
        handler.Calls[1].RequestUri.Should().Be(new Uri("https://api.bitbucket.org/2.0/repositories/ws?page=2"));
    }

    [Fact]
    public async Task Non_success_status_throws_HttpRequestException_with_bitbucket_message_when_provided()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NotFound, """{"type":"error","error":{"message":"Repository not found"}}""");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        var act = async () => await client.GetAsync<JsonElement>("repositories/ws/missing", TestContext.Current.CancellationToken);
        (await act.Should().ThrowAsync<HttpRequestException>())
            .WithMessage("Repository not found (HTTP 404 Not Found)");
    }

    // Bitbucket answers an unknown user selector with just the selector
    // ("solrevdev"), which is meaningless without the status alongside it.
    [Fact]
    public async Task Error_message_carries_the_http_status()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NotFound, """{"type":"error","error":{"message":"solrevdev"}}""");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        var act = async () => await client.GetAsync<JsonElement>("users/solrevdev", TestContext.Current.CancellationToken);
        (await act.Should().ThrowAsync<HttpRequestException>())
            .WithMessage("solrevdev (HTTP 404 Not Found)");
    }

    // Regression: error.detail is a string for most errors but an object for
    // scope failures. Typing it as string made the whole payload fail to
    // deserialize, so every 403 was reported as a bare "HTTP 403 Forbidden".
    [Fact]
    public async Task Scope_errors_name_the_missing_scopes()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Forbidden, """
            {"type":"error","error":{
              "message":"Your credentials lack one or more required privilege scopes.",
              "detail":{"required":["admin:repository:bitbucket","read:repository:bitbucket"],
                        "granted":["read:repository:bitbucket"]}}}
            """);

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        var act = async () => await client.GetAsync<JsonElement>("repositories/ws/repo/deploy-keys", TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<HttpRequestException>())
            .WithMessage("*lack one or more required privilege scopes*HTTP 403*")
            .And.Message.Should()
                .Contain("Missing token scopes: admin:repository:bitbucket")
                .And.Contain("api-tokens", "the error should say where to re-issue the token")
                .And.NotContain("read:repository:bitbucket.", "already granted scopes are not missing");
    }

    [Fact]
    public async Task String_valued_detail_still_deserializes()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.NotFound,
            """{"type":"error","error":{"message":"Not found","detail":"no such repository"}}""");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        var act = async () => await client.GetAsync<JsonElement>("repositories/ws/repo", TestContext.Current.CancellationToken);
        (await act.Should().ThrowAsync<HttpRequestException>()).WithMessage("Not found (HTTP 404 Not Found)");
    }

    [Fact]
    public async Task Deprecation_errors_point_at_the_changelog_entry()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.Gone, """
            {"type":"error","error":{"message":"CHANGE-2770 - Functionality has been deprecated",
            "data":{"announcement_url":"https://developer.atlassian.com/cloud/bitbucket/changelog#CHANGE-2770"}}}
            """);

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        var act = async () => await client.GetAsync<JsonElement>("workspaces", TestContext.Current.CancellationToken);
        (await act.Should().ThrowAsync<HttpRequestException>())
            .WithMessage("*CHANGE-2770*HTTP 410*See https://developer.atlassian.com/*");
    }

    [Fact]
    public async Task GetAsync_sends_json_accept_header()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{}");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        await client.GetAsync<JsonElement>("user", TestContext.Current.CancellationToken);

        handler.Calls[0].Headers.Accept.Select(a => a.MediaType).Should().Equal("application/json");
    }

    // Regression: the pipeline step log endpoint serves application/octet-stream
    // and answers Accept: application/json with HTTP 406 instead of falling back.
    [Theory]
    [InlineData("raw")]
    [InlineData("string")]
    [InlineData("bytes")]
    public async Task Non_json_gets_send_wildcard_accept_header(string flavour)
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "log output", "application/octet-stream");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        var endpoint = "repositories/ws/repo/pipelines/{p}/steps/{s}/log";
        switch (flavour)
        {
            case "raw": await client.GetRawAsync(endpoint, TestContext.Current.CancellationToken); break;
            case "string": await client.GetStringAsync(endpoint, TestContext.Current.CancellationToken); break;
            default: await client.GetByteArrayAsync(endpoint, TestContext.Current.CancellationToken); break;
        }

        handler.Calls[0].Headers.Accept.Select(a => a.MediaType).Should().Equal("*/*");
    }

    // Regression: the pull request diff/patch endpoints 302 to the commit-range
    // diff. HttpClient drops Authorization when it follows a redirect, so the
    // followed request came back as anonymous and Bitbucket answered
    // "You may not have access to this repository".
    [Fact]
    public async Task Redirects_are_followed_with_credentials_reapplied_on_the_same_origin()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponder(_ =>
        {
            var redirect = new HttpResponseMessage(HttpStatusCode.Found);
            redirect.Headers.Location = new Uri("https://api.bitbucket.org/2.0/repositories/ws/repo/diff/ws/repo:aaa%0Dbbb");
            return redirect;
        });
        handler.Enqueue(HttpStatusCode.OK, "diff --git a/x b/x", "text/plain");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new BasicAuthProvider("jane@example.com", "token"));

        var body = await client.GetStringAsync("repositories/ws/repo/pullrequests/1/diff", TestContext.Current.CancellationToken);

        body.Should().Be("diff --git a/x b/x");
        handler.Calls.Should().HaveCount(2);
        handler.Calls[1].RequestUri!.AbsolutePath.Should().Contain("/diff/ws/repo:");
        handler.Calls[1].Headers.Authorization.Should().NotBeNull();
        handler.Calls[1].Headers.Accept.Select(a => a.MediaType).Should().Equal("*/*");
    }

    [Fact]
    public async Task Redirects_to_another_origin_do_not_carry_credentials()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponder(_ =>
        {
            var redirect = new HttpResponseMessage(HttpStatusCode.Found);
            redirect.Headers.Location = new Uri("https://bbuseruploads.s3.amazonaws.com/artifact.zip");
            return redirect;
        });
        handler.Enqueue(HttpStatusCode.OK, "binary", "application/octet-stream");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new BasicAuthProvider("jane@example.com", "token"));

        await client.GetByteArrayAsync("repositories/ws/repo/downloads/artifact.zip", TestContext.Current.CancellationToken);

        handler.Calls.Should().HaveCount(2);
        handler.Calls[1].RequestUri!.Host.Should().Be("bbuseruploads.s3.amazonaws.com");
        handler.Calls[1].Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task Redirects_never_restore_credentials_after_leaving_the_api_origin()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponder(_ =>
        {
            var redirect = new HttpResponseMessage(HttpStatusCode.Found);
            redirect.Headers.Location = new Uri("https://downloads.example.com/first");
            return redirect;
        });
        handler.EnqueueResponder(_ =>
        {
            var redirect = new HttpResponseMessage(HttpStatusCode.Found);
            redirect.Headers.Location = new Uri("https://downloads.example.com/second");
            return redirect;
        });
        handler.Enqueue(HttpStatusCode.OK, "binary", "application/octet-stream");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new BasicAuthProvider("jane@example.com", "token"));

        await client.GetByteArrayAsync(
            "repositories/ws/repo/downloads/artifact.zip", TestContext.Current.CancellationToken);

        handler.Calls.Should().HaveCount(3);
        handler.Calls[1].Headers.Authorization.Should().BeNull();
        handler.Calls[2].Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task Absolute_urls_outside_the_api_origin_do_not_receive_credentials()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{}");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new BasicAuthProvider("jane@example.com", "token"));

        await client.GetAsync<JsonElement>(
            "https://example.com/page-two", TestContext.Current.CancellationToken);

        handler.Calls.Single().Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task CopyToAsync_streams_non_json_content_to_the_destination()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponder(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([0, 1, 2, 3, 255]),
        });

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());
        await using var destination = new MemoryStream();

        await client.CopyToAsync("downloads/file", destination, TestContext.Current.CancellationToken);

        destination.ToArray().Should().Equal(0, 1, 2, 3, 255);
        handler.Calls.Single().Headers.Accept.Select(a => a.MediaType).Should().Equal("*/*");
    }

    [Fact]
    public async Task Redirect_loops_stop_rather_than_hanging()
    {
        var handler = new FakeHttpMessageHandler();
        for (var i = 0; i < 10; i++)
        {
            handler.EnqueueResponder(_ =>
            {
                var redirect = new HttpResponseMessage(HttpStatusCode.Found);
                redirect.Headers.Location = new Uri("https://api.bitbucket.org/2.0/loop");
                return redirect;
            });
        }

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        var act = async () => await client.GetStringAsync("loop", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<HttpRequestException>();
        handler.Calls.Should().HaveCountLessThan(10);
    }

    [Fact]
    public async Task Non_success_status_falls_back_to_status_when_body_is_not_bitbucket_error()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.InternalServerError, "plain text body");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        var act = async () => await client.GetAsync<JsonElement>("anything", TestContext.Current.CancellationToken);
        (await act.Should().ThrowAsync<HttpRequestException>()).WithMessage("HTTP 500 Internal Server Error");
    }
}
