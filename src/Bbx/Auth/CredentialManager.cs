using System.Text.Json;

namespace Bbx.Auth;

public static class CredentialManager
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "bbx");

    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    public static BbxConfig Load()
    {
        if (!File.Exists(ConfigFile)) return new BbxConfig();

        try
        {
            var json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<BbxConfig>(json) ?? new BbxConfig();
        }
        catch
        {
            return new BbxConfig();
        }
    }

    public static void Save(BbxConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFile, json);

        // Secure the file (Unix only)
        if (!OperatingSystem.IsWindows())
        {
            try
            {
                File.SetUnixFileMode(ConfigFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch
            {
                // Ignore permission errors on some systems
            }
        }
    }

    public static void Clear()
    {
        if (File.Exists(ConfigFile)) File.Delete(ConfigFile);
    }

    public static bool IsAuthenticated()
    {
        var config = Load();
        return !string.IsNullOrEmpty(config.AccessToken) ||
               (!string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.AppPassword));
    }
}

public class BbxConfig
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? Username { get; set; }
    public string? AppPassword { get; set; }
    public string? DefaultWorkspace { get; set; }
    public DateTime? TokenExpiry { get; set; }
}
