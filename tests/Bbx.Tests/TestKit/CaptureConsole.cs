namespace Bbx.Tests.TestKit;

internal static class CaptureConsole
{
    public static async Task<(string Stdout, string Stderr)> RunAsync(Func<Task> action)
    {
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        Console.SetOut(stdout);
        Console.SetError(stderr);
        try
        {
            await action();
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
        return (stdout.ToString(), stderr.ToString());
    }
}
