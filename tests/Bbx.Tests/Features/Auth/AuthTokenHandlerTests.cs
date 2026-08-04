using AwesomeAssertions;
using Bbx.Auth;
using Bbx.Features.Auth.Token;
using Bbx.Tests.TestKit;

namespace Bbx.Tests.Features.Auth;

[Collection("Console")]
public class AuthTokenHandlerTests
{
    // Printed as email:token so it can be piped straight into curl -u.
    [Fact]
    public async Task Prints_the_basic_auth_pair()
    {
        var handler = new AuthTokenHandler(new CredentialManager(new InMemoryCredentialStore(new BbxConfig
        {
            AuthMethod = "api-token",
            Username = "jane@example.com",
            ApiToken = "ATATTsecret",
        })));

        var output = await handler.HandleAsync(
            new AuthTokenRequest(), TestContext.Current.CancellationToken);

        output.Should().Be("jane@example.com:ATATTsecret");
    }

    [Fact]
    public async Task Reports_not_authenticated_and_fails_when_no_token_is_stored()
    {
        var handler = new AuthTokenHandler(new CredentialManager(new InMemoryCredentialStore()));

        var act = async () => await handler.HandleAsync(
            new AuthTokenRequest(), TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<BbxUserException>()).WithMessage("*Not authenticated*");
    }
}
