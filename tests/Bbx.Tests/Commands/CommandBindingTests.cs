using System.CommandLine;
using System.CommandLine.Invocation;
using AwesomeAssertions;
using Bbx.Commands;

namespace Bbx.Tests.Commands;

public class CommandBindingTests
{
    [Fact]
    public async Task System_command_line_cancellation_reaches_bound_handlers()
    {
        var command = new Command("test");
        var seen = CancellationToken.None;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        command.SetHandler(async () =>
        {
            seen = CommandBinding.CancellationToken;
            entered.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, seen);
        });
        using var source = new CancellationTokenSource();

        var invocation = command.Parse([]).InvokeAsync(new InvocationConfiguration(), source.Token);
        await entered.Task;
        source.Cancel();
        try
        {
            await invocation;
        }
        catch (OperationCanceledException)
        {
        }

        seen.CanBeCanceled.Should().BeTrue();
        seen.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public async Task Bound_handlers_get_a_cancelable_token_even_with_no_token_supplied()
    {
        var command = new Command("test");
        var seen = CancellationToken.None;
        command.SetHandler(() =>
        {
            seen = CommandBinding.CancellationToken;
            return Task.CompletedTask;
        });

        // Passing no token is the point of the test, so xUnit1051 is off here.
#pragma warning disable xUnit1051
        await command.Parse([]).InvokeAsync(new InvocationConfiguration());
#pragma warning restore xUnit1051

        // System.CommandLine links whatever token it is handed to a source of
        // its own and cancels that from ProcessTerminationHandler, so Ctrl+C,
        // SIGINT and SIGTERM reach handlers without Program hooking any signal
        // itself. A false here means the CLI has stopped answering Ctrl+C.
        seen.CanBeCanceled.Should().BeTrue();
        seen.IsCancellationRequested.Should().BeFalse();
    }
}
