namespace Bbx.Auth;

internal sealed class NullAuthProvider : IAuthProvider
{
    public Task ApplyAsync(HttpRequestMessage request, CancellationToken ct) => Task.CompletedTask;
}
