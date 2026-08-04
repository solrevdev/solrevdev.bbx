using System.Text.Json;
using Bbx.Auth;
using Bbx.Composition;

namespace Bbx.Commands;

internal static class CommandRunner
{
    public static Task RunJsonAsync<T>(Func<Task<T>> handler) => RunAsync(
        requireAuth: true,
        async () => Console.WriteLine(JsonSerializer.Serialize(await handler(), JsonOptions.Current)));

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
