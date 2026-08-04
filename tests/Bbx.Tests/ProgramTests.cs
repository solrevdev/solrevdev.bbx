using System.Net;
using AwesomeAssertions;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Auth.Status;
using Bbx.Tests.TestKit;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Tests;

[Collection("Console")]
public sealed class ProgramTests : IDisposable
{
    private readonly IServiceProvider? _originalServices = Program.Services;

    [Fact]
    public async Task Expected_command_failure_returns_one_without_shared_exit_state()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.Unauthorized, "{}");
        var services = new ServiceCollection();
        services.AddSingleton(new CredentialManager(new InMemoryCredentialStore(new BbxConfig
        {
            Username = "jane@example.com",
            ApiToken = "expired",
        })));
        services.AddSingleton(new BitbucketClient(
            TestHttpClientFactory.Create(http), new NullAuthProvider()));
        services.AddTransient<AuthStatusHandler>();
        var provider = services.BuildServiceProvider();

        var (stdout, stderr) = await CaptureConsole.RunAsync(async () =>
        {
            var exitCode = await Program.RunAsync(["auth", "status"], provider);
            exitCode.Should().Be(1);
        });

        stdout.Should().BeEmpty();
        stderr.Should().Contain("401")
            .And.NotContain("Unhandled exception")
            .And.NotContain(" at Bbx.");
    }

    [Fact]
    public async Task Cancelling_the_supplied_token_stops_the_command_before_it_calls_the_api()
    {
        var http = new FakeHttpMessageHandler();
        http.Enqueue(HttpStatusCode.OK, """{"display_name":"Jane"}""");
        var provider = BuildAuthenticatedProvider(http);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var (stdout, stderr) = await CaptureConsole.RunAsync(async () =>
        {
            var exitCode = await Program.RunAsync(["auth", "status"], provider, cancellation.Token);
            exitCode.Should().Be(130);
        });

        http.Calls.Should().BeEmpty();
        stdout.Should().BeEmpty();
        stderr.Should().BeEmpty();
    }

    private static IServiceProvider BuildAuthenticatedProvider(FakeHttpMessageHandler http)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new CredentialManager(new InMemoryCredentialStore(new BbxConfig
        {
            Username = "jane@example.com",
            ApiToken = "token",
        })));
        services.AddSingleton(new BitbucketClient(
            TestHttpClientFactory.Create(http), new NullAuthProvider()));
        services.AddTransient<AuthStatusHandler>();
        return services.BuildServiceProvider();
    }

    public void Dispose() => Program.Services = _originalServices!;
}
