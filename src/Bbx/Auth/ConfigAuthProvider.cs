using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Auth;

// Selects the active IAuthProvider from the current on-disk config so a
// fresh login in the same process (via AuthGate) is picked up by the
// singleton BitbucketClient without rebuilding the DI container.
public sealed class ConfigAuthProvider : IAuthProvider
{
    private readonly IServiceProvider _services;
    private readonly object _lock = new();
    private IAuthProvider? _cached;

    public ConfigAuthProvider(IServiceProvider services) => _services = services;

    public Task ApplyAsync(HttpRequestMessage request, CancellationToken ct)
    {
        IAuthProvider provider;
        lock (_lock)
        {
            provider = _cached ??= Resolve();
        }
        return provider.ApplyAsync(request, ct);
    }

    public void Invalidate()
    {
        lock (_lock) { _cached = null; }
    }

    private IAuthProvider Resolve()
    {
        var creds = _services.GetRequiredService<CredentialManager>();
        var config = creds.LoadConfig();
        if (!string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.ApiToken))
        {
            return new BasicAuthProvider(config.Username, config.ApiToken);
        }
        return new NullAuthProvider();
    }
}
