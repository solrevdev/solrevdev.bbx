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
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new BbxUserException($"Error: Could not read bbx config '{_configFile}': {ex.Message}");
        }
    }

    public void Save(BbxConfig config)
    {
        Directory.CreateDirectory(_configDir);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_configDir,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        var json = JsonSerializer.Serialize(config, WriteOptions);
        var temporaryFile = Path.Combine(_configDir, $".config.{Guid.NewGuid():N}.tmp");

        try
        {
            var options = new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.WriteThrough,
            };
            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            using (var stream = new FileStream(temporaryFile, options))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(json);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryFile, _configFile, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryFile)) File.Delete(temporaryFile);
        }
    }

    public void Clear()
    {
        if (File.Exists(_configFile)) File.Delete(_configFile);
    }
}
