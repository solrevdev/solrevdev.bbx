using System.Text.Json;
using Bbx.Composition;

namespace Bbx.Commands;

internal static class CommandRunner
{
    public static async Task RunJsonAsync<T>(Func<Task<T>> handler)
    {
        try
        {
            var result = await handler();
            Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions.Indented));
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

    public static async Task RunActionAsync(Func<Task<string>> handler)
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
