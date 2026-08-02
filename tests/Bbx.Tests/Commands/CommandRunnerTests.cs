using AwesomeAssertions;
using Bbx.Auth;
using Bbx.Commands;
using Bbx.Tests.TestKit;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Tests.Commands;

[Collection("Console")]
public class CommandRunnerTests : IDisposable
{
    private readonly int _originalExitCode = Environment.ExitCode;
    private readonly IServiceProvider? _originalServices = Program.Services;

    public CommandRunnerTests()
    {
        // CommandRunner runs the auth gate first; give it a store that already
        // holds credentials so the gate passes straight through.
        var store = new InMemoryCredentialStore();
        store.Save(new BbxConfig { Username = "jane@example.com", ApiToken = "t" });

        var sc = new ServiceCollection();
        sc.AddSingleton<ICredentialStore>(store);
        sc.AddSingleton<CredentialManager>();
        Program.Services = sc.BuildServiceProvider();
    }

    public void Dispose()
    {
        Environment.ExitCode = _originalExitCode;
        Program.Services = _originalServices!;
    }

    // Regression: handlers catch their own errors and set Environment.ExitCode,
    // but Program.Main returned InvokeAsync's result, which overrode it. Every
    // failed command exited 0 and read as success to a script.
    [Fact]
    public async Task RunJsonAsync_sets_a_failing_exit_code_on_api_errors()
    {
        Environment.ExitCode = 0;

        var (stdout, stderr) = await CaptureConsole.RunAsync(() =>
            CommandRunner.RunJsonAsync<object>(() => throw new HttpRequestException("Repository not found")));

        Environment.ExitCode.Should().Be(1);
        stderr.Should().Contain("Repository not found");
        stdout.Should().BeEmpty();
    }

    [Fact]
    public async Task RunJsonAsync_sets_a_failing_exit_code_on_user_errors()
    {
        Environment.ExitCode = 0;

        var (_, stderr) = await CaptureConsole.RunAsync(() =>
            CommandRunner.RunJsonAsync<object>(() => throw new BbxUserException("Error: repo required.")));

        Environment.ExitCode.Should().Be(1);
        stderr.Should().Contain("repo required");
    }

    [Fact]
    public async Task RunJsonAsync_leaves_the_exit_code_alone_on_success()
    {
        Environment.ExitCode = 0;

        var (stdout, stderr) = await CaptureConsole.RunAsync(() =>
            CommandRunner.RunJsonAsync(() => Task.FromResult<object>(new { ok = true })));

        Environment.ExitCode.Should().Be(0);
        stdout.Should().Contain("\"ok\"");
        stderr.Should().BeEmpty();
    }

    [Fact]
    public async Task RunRawAsync_sets_a_failing_exit_code_on_api_errors()
    {
        Environment.ExitCode = 0;

        var (_, stderr) = await CaptureConsole.RunAsync(() =>
            CommandRunner.RunRawAsync(() => throw new HttpRequestException("HTTP 406 Not Acceptable")));

        Environment.ExitCode.Should().Be(1);
        stderr.Should().Contain("406");
    }
}
