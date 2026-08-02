using AwesomeAssertions;
using Bbx.Auth;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Auth;

public class CredentialManagerTests
{
    [Fact]
    public void HasCredentials_is_true_for_an_email_and_token_pair()
    {
        var mgr = new CredentialManager(new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "api-token",
            Username = "jane@example.com",
            ApiToken = "ATATT",
        }));

        mgr.HasCredentials().Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "ATATT")]
    [InlineData("jane@example.com", null)]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void HasCredentials_is_false_unless_both_halves_are_present(string? username, string? token)
    {
        var mgr = new CredentialManager(new InMemoryCredentialStore(new BbxConfig
        {
            Username = username,
            ApiToken = token,
        }));

        mgr.HasCredentials().Should().BeFalse();
    }

    [Fact]
    public void HasCredentials_is_false_for_an_empty_store()
    {
        new CredentialManager(new InMemoryCredentialStore()).HasCredentials().Should().BeFalse();
    }

    [Fact]
    public void SaveConfig_round_trips_and_ClearConfig_empties_the_store()
    {
        var store = new InMemoryCredentialStore();
        var mgr = new CredentialManager(store);

        mgr.SaveConfig(new BbxConfig
        {
            AuthMethod = "api-token",
            Username = "jane@example.com",
            ApiToken = "ATATT",
            DefaultWorkspace = "acme",
        });

        var loaded = mgr.LoadConfig();
        loaded.Username.Should().Be("jane@example.com");
        loaded.DefaultWorkspace.Should().Be("acme");

        mgr.ClearConfig();
        mgr.HasCredentials().Should().BeFalse();
        store.ClearCount.Should().Be(1);
    }
}
