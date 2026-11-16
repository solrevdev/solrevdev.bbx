using System.Text.Json;
using Bbx.Auth;
using Bbx.Composition;
using Bbx.Features.Common;

namespace Bbx.Commands;

internal static class CommandRunner
{
    public static Task RunJsonAsync<T>(Func<Task<T>> handler) => RunAsync(
        requireAuth: true,
        async () =>
        {
            object? result = await handler();
            if (result is CommandOutcome outcome)
            {
                Console.WriteLine(JsonSerializer.Serialize(NoBodyAsNull(outcome.Output), JsonOptions.Current));
                // Print first, then fail: the JSON is what a caller needs to
                // follow up (a merge task ID), even when the exit code says
                // the job is not finished.
                if (outcome.ExitCode != 0)
                    throw new BbxExitException(outcome.ExitCode, outcome.Message ?? "");
                return;
            }
            Console.WriteLine(JsonSerializer.Serialize(NoBodyAsNull(result), JsonOptions.Current));
        });

    /// <summary>
    /// A write that answers 204 has no body, so the client deserializes it to
    /// the default <see cref="JsonElement"/>. That element has ValueKind
    /// Undefined, and serializing it throws "Operation is not valid due to the
    /// current state of the object" rather than writing anything, which turns a
    /// successful call into an unexplained error. Print null instead.
    /// </summary>
    private static object? NoBodyAsNull<T>(T value) =>
        value is JsonElement { ValueKind: JsonValueKind.Undefined } ? null : value;

    public static Task RunRawAsync(Func<Task<string>> handler) => RunAsync(
        requireAuth: true,
        async () => Console.WriteLine(await handler()));

    public static Task RunStreamAsync(Func<Stream, Task> handler) => RunAsync(
        requireAuth: true,
        async () =>
        {
            await using var stdout = Console.OpenStandardOutput();
            await handler(stdout);
            await stdout.FlushAsync(CommandBinding.CancellationToken);
        });

    public static Task RunDirectAsync(Func<Task> handler) => RunAsync(requireAuth: true, handler);

    public static Task RunActionAsync(Func<Task<string>> handler) => RunAsync(
        requireAuth: true,
        async () => Console.WriteLine(await handler()));

    // Auth subcommands (login, logout, status, token, refresh, set-workspace,
    // setup-oauth) opt out of the gate: triggering OAuth login while the user
    // is mid-`bbx auth …` would be circular and surprising.
    public static Task RunActionNoGateAsync(Func<Task<string>> handler) => RunAsync(
        requireAuth: false,
        async () => Console.WriteLine(await handler()));

    private static async Task RunAsync(bool requireAuth, Func<Task> action)
    {
        if (requireAuth)
            await AuthGate.EnsureAuthenticatedAsync(Program.Services, CommandBinding.CancellationToken);
        await action();
    }

    public static bool ConfirmOrCancelStderr(string prompt)
    {
        Console.Error.Write(prompt);
        var response = Console.ReadLine()?.Trim().ToLower();
        if (response != "y" && response != "yes")
        {
            Console.Error.WriteLine("Cancelled.");
            return false;
        }
        return true;
    }
}
