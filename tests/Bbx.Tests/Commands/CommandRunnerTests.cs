using AwesomeAssertions;
using Bbx.Auth;
using Bbx.Commands;
using Bbx.Tests.TestKit;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Tests.Commands;

[Collection("Console")]
public class CommandRunnerTests : IDisposable
{
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
        Program.Services = _originalServices!;
    }

    [Fact]
    public async Task RunJsonAsync_leaves_api_errors_for_the_program_boundary()
    {
        var act = async () => await CommandRunner.RunJsonAsync<object>(
            () => throw new HttpRequestException("Repository not found"));

        (await act.Should().ThrowAsync<HttpRequestException>()).WithMessage("Repository not found");
    }

    [Fact]
    public async Task RunJsonAsync_leaves_user_errors_for_the_program_boundary()
    {
        var act = async () => await CommandRunner.RunJsonAsync<object>(
            () => throw new BbxUserException("Error: repo required."));

        (await act.Should().ThrowAsync<BbxUserException>()).WithMessage("*repo required*");
    }

    [Fact]
    public async Task RunJsonAsync_leaves_the_exit_code_alone_on_success()
    {
        var (stdout, stderr) = await CaptureConsole.RunAsync(() =>
            CommandRunner.RunJsonAsync(() => Task.FromResult<object>(new { ok = true })));

        stdout.Should().Contain("\"ok\"");
        stderr.Should().BeEmpty();
    }

    [Fact]
    public async Task RunRawAsync_leaves_api_errors_for_the_program_boundary()
    {
        var act = async () => await CommandRunner.RunRawAsync(
            () => throw new HttpRequestException("HTTP 406 Not Acceptable"));

        (await act.Should().ThrowAsync<HttpRequestException>()).WithMessage("*406*");
    }
}
