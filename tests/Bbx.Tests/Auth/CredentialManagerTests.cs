using Bbx.Auth;
using Bbx.Tests.TestKit;
using FluentAssertions;

namespace Bbx.Tests.Auth;

public class CredentialManagerTests
{
    [Fact]
    public void LoadConfig_returns_empty_when_store_is_empty()
    {
        var store = new InMemoryCredentialStore();
        var creds = new CredentialManager(store);

        var config = creds.LoadConfig();

        config.Should().NotBeNull();
        config.AuthMethod.Should().BeNull();
        config.Username.Should().BeNull();
    }

    [Fact]
    public void SaveConfig_round_trips_through_store()
    {
        var store = new InMemoryCredentialStore();
        var creds = new CredentialManager(store);

        creds.SaveConfig(new BbxConfig
        {
            AuthMethod = "api-token",
            Username = "jane@example.com",
            ApiToken = "ATATT3xFfGF0",
            DefaultWorkspace = "ws",
        });

        store.SaveCount.Should().Be(1);
        var snapshot = store.Snapshot();
        snapshot!.AuthMethod.Should().Be("api-token");
        snapshot.Username.Should().Be("jane@example.com");
        snapshot.ApiToken.Should().Be("ATATT3xFfGF0");
        snapshot.DefaultWorkspace.Should().Be("ws");
    }

    [Fact]
    public void LoadConfig_migrates_legacy_AppPassword_into_ApiToken_and_writes_back()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            Username = "jane@example.com",
            AppPassword = "ATATT3xFfGF0",
            DefaultWorkspace = "ws",
        });
        var creds = new CredentialManager(store);

        var config = creds.LoadConfig();

        config.AuthMethod.Should().Be("api-token");
        config.ApiToken.Should().Be("ATATT3xFfGF0");
        config.AppPassword.Should().BeNull();

        store.SaveCount.Should().Be(1);
        var snapshot = store.Snapshot();
        snapshot!.AuthMethod.Should().Be("api-token");
        snapshot.ApiToken.Should().Be("ATATT3xFfGF0");
        snapshot.AppPassword.Should().BeNull();
    }

    [Fact]
    public void LoadConfig_migration_is_one_shot_and_does_not_resave_on_subsequent_loads()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            Username = "jane@example.com",
            AppPassword = "ATATT3xFfGF0",
        });
        var creds = new CredentialManager(store);

        _ = creds.LoadConfig();
        _ = creds.LoadConfig();
        _ = creds.LoadConfig();

        store.SaveCount.Should().Be(1);
    }

    [Fact]
    public void LoadConfig_marks_oauth_when_both_access_and_refresh_tokens_present()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AccessToken = "ya29.x",
            RefreshToken = "1//09Q.x",
        });
        var creds = new CredentialManager(store);

        var config = creds.LoadConfig();

        config.AuthMethod.Should().Be("oauth");
    }

    [Fact]
    public void HasCredentials_true_when_api_token_present()
    {
        var store = new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "api-token",
            Username = "jane@example.com",
            ApiToken = "ATATT3xFfGF0",
        });
        var creds = new CredentialManager(store);

        creds.HasCredentials().Should().BeTrue();
    }

    [Fact]
    public void HasCredentials_false_when_store_is_empty()
    {
        var creds = new CredentialManager(new InMemoryCredentialStore());
        creds.HasCredentials().Should().BeFalse();
    }

    [Fact]
    public void ClearConfig_delegates_to_store()
    {
        var store = new InMemoryCredentialStore(new BbxConfig { Username = "u", ApiToken = "t" });
        var creds = new CredentialManager(store);

        creds.ClearConfig();

        store.ClearCount.Should().Be(1);
        store.Snapshot().Should().BeNull();
    }
}
