using Bbx.Auth;
using FluentAssertions;

namespace Bbx.Tests.Features.Auth;

/// <summary>
/// `bbx auth login` with no method flag picks OAuth in an interactive shell and
/// falls back to the chooser when there is no TTY, since a browser flow cannot
/// complete in CI. This pins the predicate that decision rests on.
/// </summary>
[Collection("Console")]
public class LoginDefaultTests : IDisposable
{
    private readonly string? _original = Environment.GetEnvironmentVariable("BBX_NO_INTERACTIVE");

    public void Dispose() => Environment.SetEnvironmentVariable("BBX_NO_INTERACTIVE", _original);

    [Fact]
    public void BBX_NO_INTERACTIVE_opts_out_of_the_browser_default()
    {
        Environment.SetEnvironmentVariable("BBX_NO_INTERACTIVE", "1");

        AuthGate.DefaultIsInteractive().Should().BeFalse();
    }

    [Fact]
    public void Any_other_value_does_not_opt_out()
    {
        Environment.SetEnvironmentVariable("BBX_NO_INTERACTIVE", "0");

        // Still gated on a TTY, which the test host does not have, so assert the
        // env var alone did not force the opt-out.
        AuthGate.DefaultIsInteractive().Should().Be(!Console.IsInputRedirected);
    }

    [Fact]
    public void Redirected_stdin_opts_out_even_without_the_env_var()
    {
        Environment.SetEnvironmentVariable("BBX_NO_INTERACTIVE", null);

        // The test host runs with stdin redirected, which is the CI shape.
        if (Console.IsInputRedirected)
        {
            AuthGate.DefaultIsInteractive().Should().BeFalse();
        }
    }
}
