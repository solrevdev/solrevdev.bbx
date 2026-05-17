using System.Net;
using Bbx.Auth;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Auth;

public class OAuthFlowTests
{
    [Fact]
    public async Task Happy_path_exchanges_code_for_tokens()
    {
        var handler = new FakeHttpMessageHandler();
        handler.Enqueue(HttpStatusCode.OK,
            """{"access_token":"at1","refresh_token":"rt1","expires_in":7200,"token_type":"bearer","scopes":"account repository"}""");
        using var http = new HttpClient(handler);

        var port = FreePort.Pick();
        var browser = new StubBrowserLauncher();
        browser.OnLaunch = url => _ = Task.Run(async () =>
        {
            var state = ExtractState(url);
            using var client = new HttpClient();
            await client.GetAsync($"http://127.0.0.1:{port}/callback?code=auth-code-1&state={state}");
        });

        var flow = new OAuthFlow(browser, http);
        var token = await flow.RunAsync(new OAuthFlowOptions
        {
            ClientId = "cid",
            ClientSecret = "csec",
            Port = port,
            Timeout = TimeSpan.FromSeconds(10),
        }, CancellationToken.None);

        token.AccessToken.Should().Be("at1");
        token.RefreshToken.Should().Be("rt1");
        token.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddSeconds(7200), TimeSpan.FromSeconds(5));

        browser.LaunchedUrls.Should().ContainSingle();
        browser.LaunchedUrls[0].Should().StartWith("https://bitbucket.org/site/oauth2/authorize?client_id=cid&response_type=code&state=");

        handler.Calls.Should().ContainSingle();
        var exchange = handler.Calls[0];
        exchange.RequestUri.Should().Be(new Uri("https://bitbucket.org/site/oauth2/access_token"));
        handler.CallBodies[0].Should().Contain("grant_type=authorization_code");
        handler.CallBodies[0].Should().Contain("code=auth-code-1");
    }

    [Fact]
    public async Task Rejects_state_mismatch()
    {
        var handler = new FakeHttpMessageHandler();
        using var http = new HttpClient(handler);

        var port = FreePort.Pick();
        var browser = new StubBrowserLauncher();
        browser.OnLaunch = url => { _ = Task.Run(async () =>
        {
            using var client = new HttpClient();
            await client.GetAsync($"http://127.0.0.1:{port}/callback?code=ignored&state=not-the-state");
        }); };

        var flow = new OAuthFlow(browser, http);
        var act = async () => await flow.RunAsync(new OAuthFlowOptions
        {
            ClientId = "cid",
            ClientSecret = "csec",
            Port = port,
            Timeout = TimeSpan.FromSeconds(10),
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*state mismatch*");
        handler.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Times_out_when_no_callback_arrives()
    {
        var handler = new FakeHttpMessageHandler();
        using var http = new HttpClient(handler);

        var port = FreePort.Pick();
        var browser = new StubBrowserLauncher(); // no callback issued

        var flow = new OAuthFlow(browser, http);
        var act = async () => await flow.RunAsync(new OAuthFlowOptions
        {
            ClientId = "cid",
            ClientSecret = "csec",
            Port = port,
            Timeout = TimeSpan.FromMilliseconds(250),
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*OAuth callback timed out*");
    }

    [Fact]
    public async Task Surfaces_oauth_error_returned_in_callback()
    {
        var handler = new FakeHttpMessageHandler();
        using var http = new HttpClient(handler);

        var port = FreePort.Pick();
        var browser = new StubBrowserLauncher();
        browser.OnLaunch = url => { _ = Task.Run(async () =>
        {
            using var client = new HttpClient();
            await client.GetAsync($"http://127.0.0.1:{port}/callback?error=access_denied&error_description=user+declined");
        }); };

        var flow = new OAuthFlow(browser, http);
        var act = async () => await flow.RunAsync(new OAuthFlowOptions
        {
            ClientId = "cid",
            ClientSecret = "csec",
            Port = port,
            Timeout = TimeSpan.FromSeconds(10),
        }, CancellationToken.None);

        (await act.Should().ThrowAsync<BbxUserException>())
            .WithMessage("*access_denied*");
        handler.Calls.Should().BeEmpty();
    }

    private static string ExtractState(string url)
    {
        var query = new Uri(url).Query.TrimStart('?');
        foreach (var pair in query.Split('&'))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0] == "state") return Uri.UnescapeDataString(parts[1]);
        }
        throw new InvalidOperationException("state not found in URL: " + url);
    }
}
