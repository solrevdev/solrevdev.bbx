using System.Net.Http.Headers;
using System.Text;

namespace Bbx.Auth;

internal sealed class BasicAuthProvider(string username, string secret) : IAuthProvider
{
    private readonly string _credentials = Convert.ToBase64String(
        Encoding.UTF8.GetBytes($"{username}:{secret}"));

    public Task ApplyAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _credentials);
        return Task.CompletedTask;
    }
}
