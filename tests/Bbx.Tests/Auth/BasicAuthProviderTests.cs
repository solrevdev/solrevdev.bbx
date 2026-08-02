using System.Text;
using AwesomeAssertions;
using Bbx.Auth;

namespace Bbx.Tests.Auth;

public class BasicAuthProviderTests
{
    [Fact]
    public async Task ApplyAsync_sets_basic_auth_header_with_base64_username_colon_secret()
    {
        var provider = new BasicAuthProvider("jane@example.com", "ATATT3xFfGF0");
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.bitbucket.org/2.0/user");

        await provider.ApplyAsync(request, TestContext.Current.CancellationToken);

        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Basic");
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!));
        decoded.Should().Be("jane@example.com:ATATT3xFfGF0");
    }
}
