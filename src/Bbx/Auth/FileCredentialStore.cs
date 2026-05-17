using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bbx.Auth;

public sealed class FileCredentialStore : ICredentialStore
{
    private static readonly string DefaultConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "bbx");

    private static readonly string DefaultConfigFile = Path.Combine(DefaultConfigDir, "config.json");

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _configDir;
    private readonly string _configFile;

    public FileCredentialStore() : this(DefaultConfigDir, DefaultConfigFile) { }

    public FileCredentialStore(string configDir, string configFile)
    {
        _configDir = configDir;
        _configFile = configFile;
    }

    public BbxConfig Load()
    {
        if (!File.Exists(_configFile)) return new BbxConfig();

        try
        {
            var json = File.ReadAllText(_configFile);
            return JsonSerializer.Deserialize<BbxConfig>(json) ?? new BbxConfig();
        }
        catch
        {
            return new BbxConfig();
        }
    }

    public void Save(BbxConfig config)
    {
        Directory.CreateDirectory(_configDir);
        var json = JsonSerializer.Serialize(config, WriteOptions);
        File.WriteAllText(_configFile, json);

        if (!OperatingSystem.IsWindows())
        {
            try
            {
                File.SetUnixFileMode(_configFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch
            {
            }
        }
    }

    public void Clear()
    {
        if (File.Exists(_configFile)) File.Delete(_configFile);
    }
}
