using System.Net;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Tests.TestKit;
using FluentAssertions;

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

        await client.GetAsync<JsonElement>("/user");

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

        await client.GetAsync<JsonElement>("repositories/ws/repo");

        handler.Calls[0].RequestUri.Should().Be(new Uri("https://api.bitbucket.org/2.0/repositories/ws/repo"));
    }

    [Fact]
    public async Task BasicAuthProvider_applies_authorization_header()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{}");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new BasicAuthProvider("jane@example.com", "ATATT3xFfGF0"));

        await client.GetAsync<JsonElement>("user");

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

        await client.GetAsync<JsonElement>("user");

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
        await foreach (var element in client.GetPaginatedAsync<JsonElement>("repositories/ws"))
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

        var act = async () => await client.GetAsync<JsonElement>("repositories/ws/missing");
        (await act.Should().ThrowAsync<HttpRequestException>()).WithMessage("Repository not found");
    }

    [Fact]
    public async Task GetAsync_sends_json_accept_header()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK, "{}");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        await client.GetAsync<JsonElement>("user");

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
            case "raw": await client.GetRawAsync(endpoint); break;
            case "string": await client.GetStringAsync(endpoint); break;
            default: await client.GetByteArrayAsync(endpoint); break;
        }

        handler.Calls[0].Headers.Accept.Select(a => a.MediaType).Should().Equal("*/*");
    }

    [Fact]
    public async Task Non_success_status_falls_back_to_status_when_body_is_not_bitbucket_error()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.InternalServerError, "plain text body");

        using var http = TestHttpClientFactory.Create(handler);
        var client = new BitbucketClient(http, new NullAuthProvider());

        var act = async () => await client.GetAsync<JsonElement>("anything");
        (await act.Should().ThrowAsync<HttpRequestException>()).WithMessage("HTTP 500 Internal Server Error");
    }
}
