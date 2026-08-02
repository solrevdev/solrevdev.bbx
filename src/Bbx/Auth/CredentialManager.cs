namespace Bbx.Auth;

public sealed class CredentialManager
{
    private readonly ICredentialStore _store;

    public CredentialManager(ICredentialStore store) => _store = store;

    public BbxConfig LoadConfig() => _store.Load();

    public void SaveConfig(BbxConfig config) => _store.Save(config);

    public void ClearConfig() => _store.Clear();

    public bool HasCredentials()
    {
        var config = LoadConfig();
        return !string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.ApiToken);
    }
}

/// <summary>
/// On-disk shape of <c>~/.config/bbx/config.json</c>.
/// </summary>
/// <remarks>
/// Atlassian API tokens are the only supported credential. They go over the
/// wire as HTTP Basic (email:token), the same shape app passwords used.
/// Unknown properties are ignored on load, so a config written by a build that
/// still had OAuth is read for its Username / ApiToken / DefaultWorkspace, and
/// the stale fields are dropped on the next save.
/// </remarks>
public class BbxConfig
{
    public string? AuthMethod { get; set; }
    public string? Username { get; set; }
    public string? ApiToken { get; set; }
    public string? DefaultWorkspace { get; set; }
}
