using Bbx.Auth;
using Bbx.Features.Auth.SetupOAuth;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Features.Auth;

[Collection("Console")]
public class SetupOAuthHandlerTests
{
    [Fact]
    public async Task Prints_walkthrough_with_default_workspace_when_set_and_does_not_open_browser_by_default()
    {
        var store = new InMemoryCredentialStore(new BbxConfig { DefaultWorkspace = "acme" });
        var creds = new CredentialManager(store);
        var browser = new StubBrowserLauncher();
        var handler = new SetupOAuthHandler(creds, browser);

        var (stdout, _) = await CaptureConsole.RunAsync(() =>
            handler.HandleAsync(new SetupOAuthRequest(Open: false), CancellationToken.None));

        stdout.Should().Contain("https://bitbucket.org/acme/workspace/settings/api");
        stdout.Should().Contain("http://localhost:53682/callback");
        stdout.Should().Contain("bbx auth login --oauth");
        browser.LaunchedUrls.Should().BeEmpty();
    }

    [Fact]
    public async Task Opens_browser_when_open_is_true()
    {
        var store = new InMemoryCredentialStore(new BbxConfig { DefaultWorkspace = "acme" });
        var creds = new CredentialManager(store);
        var browser = new StubBrowserLauncher();
        var handler = new SetupOAuthHandler(creds, browser);

        await CaptureConsole.RunAsync(() =>
            handler.HandleAsync(new SetupOAuthRequest(Open: true), CancellationToken.None));

        browser.LaunchedUrls.Should().ContainSingle()
            .Which.Should().Be("https://bitbucket.org/acme/workspace/settings/api");
    }

    [Fact]
    public async Task Uses_placeholder_workspace_when_default_is_unset()
    {
        var store = new InMemoryCredentialStore();
        var creds = new CredentialManager(store);
        var browser = new StubBrowserLauncher();
        var handler = new SetupOAuthHandler(creds, browser);

        var (stdout, _) = await CaptureConsole.RunAsync(() =>
            handler.HandleAsync(new SetupOAuthRequest(Open: false), CancellationToken.None));

        stdout.Should().Contain("<your-workspace>");
    }
}
