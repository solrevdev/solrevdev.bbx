using Bbx.Auth;
using Bbx.Features.Auth.Token;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Auth;

[Collection("Console")]
public class AuthTokenHandlerTests : IDisposable
{
    private readonly int _originalExitCode = Environment.ExitCode;

    public void Dispose() => Environment.ExitCode = _originalExitCode;

    // Printed as email:token so it can be piped straight into curl -u.
    [Fact]
    public async Task Prints_the_basic_auth_pair()
    {
        var handler = new AuthTokenHandler(new CredentialManager(new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "api-token",
            Username = "jane@example.com",
            ApiToken = "ATATTsecret",
        })));

        var (stdout, stderr) = await CaptureConsole.RunAsync(() =>
            handler.HandleAsync(new AuthTokenRequest(), CancellationToken.None));

        stdout.Trim().Should().Be("jane@example.com:ATATTsecret");
        stderr.Should().BeEmpty();
    }

    [Fact]
    public async Task Reports_not_authenticated_and_fails_when_no_token_is_stored()
    {
        Environment.ExitCode = 0;
        var handler = new AuthTokenHandler(new CredentialManager(new InMemoryCredentialStore()));

        var (stdout, stderr) = await CaptureConsole.RunAsync(() =>
            handler.HandleAsync(new AuthTokenRequest(), CancellationToken.None));

        stdout.Should().BeEmpty();
        stderr.Should().Contain("Not authenticated");
        Environment.ExitCode.Should().Be(1);
    }
}
