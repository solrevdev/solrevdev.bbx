using AwesomeAssertions;
using Bbx.Auth;

namespace Bbx.Tests.Auth;

public class NullAuthProviderTests
{
    [Fact]
    public async Task ApplyAsync_leaves_request_authorization_header_unset()
    {
        var provider = new NullAuthProvider();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.bitbucket.org/2.0/user");

        await provider.ApplyAsync(request, TestContext.Current.CancellationToken);

        request.Headers.Authorization.Should().BeNull();
    }
}
