namespace Bbx.Auth;

public sealed class CredentialManager
{
    private static readonly Lazy<CredentialManager> Default =
        new(() => new CredentialManager(new FileCredentialStore()));

    private readonly ICredentialStore _store;

    public CredentialManager(ICredentialStore store) => _store = store;

    public BbxConfig LoadConfig()
    {
        var config = _store.Load();
        if (TryMigrate(config))
        {
            try
            {
                _store.Save(config);
            }
            catch
            {
                // Migration write-back is best-effort: if the on-disk file
                // can't be rewritten (read-only fs, perms), keep the
                // in-memory migrated shape so the current run still works.
                // The next successful Save call will persist the new shape.
            }
        }
        return config;
    }

    public void SaveConfig(BbxConfig config) => _store.Save(config);

    public void ClearConfig() => _store.Clear();

    public bool HasCredentials()
    {
        var config = LoadConfig();
        return !string.IsNullOrEmpty(config.AccessToken) ||
               (!string.IsNullOrEmpty(config.Username)
                   && !string.IsNullOrEmpty(config.ApiToken ?? config.AppPassword));
    }

    private static bool TryMigrate(BbxConfig config)
    {
        if (!string.IsNullOrEmpty(config.AuthMethod)) return false;

        if (!string.IsNullOrEmpty(config.AppPassword))
        {
            config.AuthMethod = "api-token";
            config.ApiToken = config.AppPassword;
            config.AppPassword = null;
            return true;
        }

        if (!string.IsNullOrEmpty(config.AccessToken) && !string.IsNullOrEmpty(config.RefreshToken))
        {
            config.AuthMethod = "oauth";
            return true;
        }

        return false;
    }

    public static BbxConfig Load() => Default.Value.LoadConfig();
    public static void Save(BbxConfig config) => Default.Value.SaveConfig(config);
    public static void Clear() => Default.Value.ClearConfig();
    public static bool IsAuthenticated() => Default.Value.HasCredentials();
}

public class BbxConfig
{
    public string? AuthMethod { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset? TokenExpiry { get; set; }
    public string? OAuthClientId { get; set; }
    public string? OAuthClientSecret { get; set; }
    public string? Username { get; set; }
    public string? ApiToken { get; set; }
    public string? DefaultWorkspace { get; set; }

    public string? AppPassword { get; set; }
}
