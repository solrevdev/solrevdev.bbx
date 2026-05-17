using System.Text;

namespace Bbx.Auth;

public static class SecretInput
{
    public static string ReadSecret()
    {
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine()?.TrimEnd('\r', '\n') ?? string.Empty;
        }

        var sb = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
            {
                sb.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                sb.Append(key.KeyChar);
            }
        }
        Console.WriteLine();
        return sb.ToString();
    }
}
