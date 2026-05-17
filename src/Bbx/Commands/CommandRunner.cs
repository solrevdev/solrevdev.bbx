using System.Text.Json;
using Bbx.Auth;
using Bbx.Composition;

namespace Bbx.Commands;

internal static class CommandRunner
{
    public static async Task RunJsonAsync<T>(Func<Task<T>> handler)
    {
        try
        {
            await AuthGate.EnsureAuthenticatedAsync(Program.Services, CancellationToken.None);
            var result = await handler();
            Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions.Current));
        }
        catch (BbxUserException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.ExitCode = 1;
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    public static async Task RunRawAsync(Func<Task<string>> handler)
    {
        try
        {
            await AuthGate.EnsureAuthenticatedAsync(Program.Services, CancellationToken.None);
            var output = await handler();
            Console.WriteLine(output);
        }
        catch (BbxUserException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.ExitCode = 1;
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    public static async Task RunBinaryAsync(Func<Task<byte[]>> handler)
    {
        try
        {
            await AuthGate.EnsureAuthenticatedAsync(Program.Services, CancellationToken.None);
            var bytes = await handler();
            await using var stdout = Console.OpenStandardOutput();
            await stdout.WriteAsync(bytes, CancellationToken.None);
            await stdout.FlushAsync(CancellationToken.None);
        }
        catch (BbxUserException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.ExitCode = 1;
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    public static async Task RunActionAsync(Func<Task<string>> handler)
    {
        try
        {
            await AuthGate.EnsureAuthenticatedAsync(Program.Services, CancellationToken.None);
            var message = await handler();
            Console.WriteLine(message);
        }
        catch (BbxUserException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.ExitCode = 1;
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    // Auth subcommands (login, logout, status, token, refresh, set-workspace,
    // setup-oauth) opt out of the gate: triggering OAuth login while the user
    // is mid-`bbx auth …` would be circular and surprising.
    public static async Task RunActionNoGateAsync(Func<Task<string>> handler)
    {
        try
        {
            var message = await handler();
            Console.WriteLine(message);
        }
        catch (BbxUserException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.ExitCode = 1;
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    public static bool ConfirmOrCancel(string prompt)
    {
        Console.Write(prompt);
        var response = Console.ReadLine()?.Trim().ToLower();
        if (response != "y" && response != "yes")
        {
            Console.WriteLine("Cancelled.");
            return false;
        }
        return true;
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
