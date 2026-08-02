using Bbx.Auth;
using Bbx.Features.Auth.LoginApiToken;
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
        var services = BuildServices(store);

        await AuthGate.EnsureAuthenticatedAsync(services, CancellationToken.None);

        store.SaveCount.Should().Be(0, "an authenticated run must not rewrite the config");
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
            var services = BuildServices(new InMemoryCredentialStore());

            var act = async () => await AuthGate.EnsureAuthenticatedAsync(services, CancellationToken.None);

            (await act.Should().ThrowAsync<BbxUserException>())
                .WithMessage("*Not authenticated*")
                .And.Message.Should().Contain("api-tokens", "the error should say where to get one");
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
        var previousVar = Environment.GetEnvironmentVariable("BBX_NO_INTERACTIVE");
        var previousHook = AuthGate.IsInteractive;
        Environment.SetEnvironmentVariable("BBX_NO_INTERACTIVE", null);
        AuthGate.IsInteractive = () => false;
        try
        {
            var services = BuildServices(new InMemoryCredentialStore());

            var act = async () => await AuthGate.EnsureAuthenticatedAsync(services, CancellationToken.None);

            await act.Should().ThrowAsync<BbxUserException>().WithMessage("*Not authenticated*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("BBX_NO_INTERACTIVE", previousVar);
            AuthGate.IsInteractive = previousHook;
        }
    }

    // First run in a terminal prompts for a token rather than failing, then the
    // original command carries on with the credential it just stored.
    [Fact]
    public async Task Prompts_for_a_token_when_interactive_and_stores_it()
    {
        var previousHook = AuthGate.IsInteractive;
        AuthGate.IsInteractive = () => true;
        var originalIn = Console.In;
        try
        {
            var store = new InMemoryCredentialStore();
            var services = BuildServices(store);
            Console.SetIn(new StringReader("jane@example.com\nATATTsecret\n"));

            await CaptureConsole.RunAsync(() =>
                AuthGate.EnsureAuthenticatedAsync(services, CancellationToken.None));

            var saved = store.Load();
            saved.Username.Should().Be("jane@example.com");
            saved.ApiToken.Should().Be("ATATTsecret");
            saved.AuthMethod.Should().Be("api-token");
        }
        finally
        {
            Console.SetIn(originalIn);
            AuthGate.IsInteractive = previousHook;
        }
    }

    [Fact]
    public async Task Invalidates_the_cached_provider_so_the_command_sees_the_new_credential()
    {
        var previousHook = AuthGate.IsInteractive;
        AuthGate.IsInteractive = () => true;
        var originalIn = Console.In;
        try
        {
            var store = new InMemoryCredentialStore();
            var services = BuildServices(store);

            // Resolve once with no credentials so the provider caches NullAuthProvider.
            var provider = services.GetRequiredService<IAuthProvider>();
            using var before = new HttpRequestMessage(HttpMethod.Get, "https://api.bitbucket.org/2.0/user");
            await provider.ApplyAsync(before, CancellationToken.None);
            before.Headers.Authorization.Should().BeNull();

            Console.SetIn(new StringReader("jane@example.com\nATATTsecret\n"));
            await CaptureConsole.RunAsync(() =>
                AuthGate.EnsureAuthenticatedAsync(services, CancellationToken.None));

            using var after = new HttpRequestMessage(HttpMethod.Get, "https://api.bitbucket.org/2.0/user");
            await provider.ApplyAsync(after, CancellationToken.None);
            after.Headers.Authorization.Should().NotBeNull();
            after.Headers.Authorization!.Scheme.Should().Be("Basic");
        }
        finally
        {
            Console.SetIn(originalIn);
            AuthGate.IsInteractive = previousHook;
        }
    }

    private static IServiceProvider BuildServices(InMemoryCredentialStore store)
        => BuildServices(store, Verified());

    // LoginApiTokenHandler verifies the credential against /user before saving.
    private static FakeHttpMessageHandler Verified()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(System.Net.HttpStatusCode.OK,
            """{"display_name":"Jane Doe","username":"jane"}""");
        return http;
    }

    private static IServiceProvider BuildServices(InMemoryCredentialStore store, FakeHttpMessageHandler http)
    {
        var sc = new ServiceCollection();
        sc.AddSingleton(_ => TestHttpClientFactory.Create(http));
        sc.AddSingleton<ICredentialStore>(store);
        sc.AddSingleton<CredentialManager>();
        sc.AddSingleton<ConfigAuthProvider>();
        sc.AddSingleton<IAuthProvider>(sp => sp.GetRequiredService<ConfigAuthProvider>());
        sc.AddTransient<LoginApiTokenHandler>();
        return sc.BuildServiceProvider();
    }
}
