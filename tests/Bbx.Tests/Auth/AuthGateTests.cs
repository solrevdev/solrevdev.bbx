using System.Net;
using Bbx.Auth;
using Bbx.Features.Auth.LoginOAuth;
using Bbx.Features.Auth.SetupOAuth;
using Bbx.Tests.TestKit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Tests.Auth;

[Collection("Console")]
public class AuthGateTests
{
    [Fact]
    public async Task Returns_immediately_when_credentials_present()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "api-token",
            Username = "jane@example.com",
            ApiToken = "ATATT",
        });
        var fakeHttp = new FakeHttpMessageHandler();
        var browser = new StubBrowserLauncher();
        var services = BuildServices(store, fakeHttp, browser);

        await AuthGate.EnsureAuthenticatedAsync(services, CancellationToken.None);

        fakeHttp.Calls.Should().BeEmpty();
        browser.LaunchedUrls.Should().BeEmpty();
    }

    [Fact]
    public async Task Falls_back_to_error_when_BBX_NO_INTERACTIVE_is_set()
    {
        var previousVar = Environment.GetEnvironmentVariable("BBX_NO_INTERACTIVE");
        var previousHook = AuthGate.IsInteractive;
        Environment.SetEnvironmentVariable("BBX_NO_INTERACTIVE", "1");
        AuthGate.IsInteractive = AuthGate.DefaultIsInteractive;
        try
        {
            var store = new InMemoryCredentialStore();
            var fakeHttp = new FakeHttpMessageHandler();
            var browser = new StubBrowserLauncher();
            var services = BuildServices(store, fakeHttp, browser);

            var act = async () => await AuthGate.EnsureAuthenticatedAsync(services, CancellationToken.None);

            await act.Should().ThrowAsync<BbxUserException>().WithMessage("*Not authenticated*");
            browser.LaunchedUrls.Should().BeEmpty();
            fakeHttp.Calls.Should().BeEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable("BBX_NO_INTERACTIVE", previousVar);
            AuthGate.IsInteractive = previousHook;
        }
    }

    [Fact]
    public async Task Falls_back_to_error_when_stdin_is_not_a_tty()
    {
        var previousHook = AuthGate.IsInteractive;
        AuthGate.IsInteractive = () => false;
        try
        {
            var store = new InMemoryCredentialStore();
            var fakeHttp = new FakeHttpMessageHandler();
            var browser = new StubBrowserLauncher();
            var services = BuildServices(store, fakeHttp, browser);

            var act = async () => await AuthGate.EnsureAuthenticatedAsync(services, CancellationToken.None);

            await act.Should().ThrowAsync<BbxUserException>().WithMessage("*Not authenticated*");
        }
        finally
        {
            AuthGate.IsInteractive = previousHook;
        }
    }

    [Fact]
    public async Task Happy_auto_launch_with_existing_consumer_runs_flow_and_invalidates_provider()
    {
        var previousHook = AuthGate.IsInteractive;
        var previousPort = AuthGate.LoginPort;
        AuthGate.IsInteractive = () => true;
        var port = FreePort.Pick();
        AuthGate.LoginPort = port;

        try
        {
            var store = new InMemoryCredentialStore(new BbxConfig
            {
                OAuthClientId = "cid",
                OAuthClientSecret = "csec",
            });
            var fakeHttp = new FakeHttpMessageHandler();
            fakeHttp.Enqueue(HttpStatusCode.OK,
                """{"access_token":"at1","refresh_token":"rt1","expires_in":7200}""");
            fakeHttp.Enqueue(HttpStatusCode.OK,
                """{"display_name":"Jane Doe","username":"jane"}""");

            var browser = new StubBrowserLauncher();
            browser.OnLaunch = url => { _ = Task.Run(async () =>
            {
                var state = ExtractState(url);
                using var http = new HttpClient();
                await http.GetAsync($"http://127.0.0.1:{port}/callback?code=auth-code&state={state}");
            }); };

            var services = BuildServices(store, fakeHttp, browser);

            await CaptureConsole.RunAsync(() =>
                AuthGate.EnsureAuthenticatedAsync(services, CancellationToken.None));

            var snapshot = store.Snapshot();
            snapshot.Should().NotBeNull();
            snapshot!.AuthMethod.Should().Be("oauth");
            snapshot.AccessToken.Should().Be("at1");
            snapshot.RefreshToken.Should().Be("rt1");
            snapshot.Username.Should().Be("jane");

            // ConfigAuthProvider was Invalidated by AuthGate, so it now
            // resolves to the OAuth provider rather than the pre-login
            // NullAuthProvider.
            var auth = services.GetRequiredService<IAuthProvider>();
            using var probe = new HttpRequestMessage(HttpMethod.Get, "https://api.bitbucket.org/2.0/user");
            await auth.ApplyAsync(probe, CancellationToken.None);
            probe.Headers.Authorization!.Scheme.Should().Be("Bearer");
            probe.Headers.Authorization.Parameter.Should().Be("at1");
        }
        finally
        {
            AuthGate.IsInteractive = previousHook;
            AuthGate.LoginPort = previousPort;
        }
    }

    private static IServiceProvider BuildServices(
        InMemoryCredentialStore store,
        FakeHttpMessageHandler fakeHttp,
        StubBrowserLauncher browser)
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<ICredentialStore>(store);
        sc.AddSingleton<CredentialManager>();
        sc.AddSingleton(_ => new HttpClient(fakeHttp));
        sc.AddSingleton<IBrowserLauncher>(browser);
        sc.AddSingleton(sp => new OAuthFlow(
            sp.GetRequiredService<IBrowserLauncher>(),
            sp.GetRequiredService<HttpClient>()));
        sc.AddSingleton(sp => new OAuthAuthProvider(
            sp.GetRequiredService<CredentialManager>(),
            sp.GetRequiredService<HttpClient>()));
        sc.AddSingleton<ConfigAuthProvider>();
        sc.AddSingleton<IAuthProvider>(sp => sp.GetRequiredService<ConfigAuthProvider>());
        sc.AddTransient<SetupOAuthHandler>();
        sc.AddTransient<LoginOAuthHandler>();
        return sc.BuildServiceProvider();
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
