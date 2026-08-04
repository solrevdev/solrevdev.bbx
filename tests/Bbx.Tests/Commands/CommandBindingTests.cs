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
}
