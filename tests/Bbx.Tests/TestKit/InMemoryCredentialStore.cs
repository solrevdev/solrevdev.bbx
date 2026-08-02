using Bbx.Auth;

namespace Bbx.Tests.TestKit;

internal sealed class InMemoryCredentialStore : ICredentialStore
{
    private BbxConfig? _config;
    public int SaveCount { get; private set; }
    public int ClearCount { get; private set; }

    public InMemoryCredentialStore() { }
    public InMemoryCredentialStore(BbxConfig seed) => _config = Clone(seed);

    public BbxConfig Load() => _config is null ? new BbxConfig() : Clone(_config);

    public void Save(BbxConfig config)
    {
        _config = Clone(config);
        SaveCount++;
    }

    public void Clear()
    {
        _config = null;
        ClearCount++;
    }

    public BbxConfig? Snapshot() => _config is null ? null : Clone(_config);

    private static BbxConfig Clone(BbxConfig source) => new()
    {
        AuthMethod = source.AuthMethod,
        Username = source.Username,
        ApiToken = source.ApiToken,
        DefaultWorkspace = source.DefaultWorkspace,
    };
}
