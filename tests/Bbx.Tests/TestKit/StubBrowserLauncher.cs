using Bbx.Auth;

namespace Bbx.Tests.TestKit;

internal sealed class StubBrowserLauncher : IBrowserLauncher
{
    public List<string> LaunchedUrls { get; } = new();
    public Action<string>? OnLaunch { get; set; }

    public void Launch(string url)
    {
        LaunchedUrls.Add(url);
        OnLaunch?.Invoke(url);
    }
}
