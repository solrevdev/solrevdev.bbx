namespace Bbx.Auth;

public interface IAuthProvider
{
    Task ApplyAsync(HttpRequestMessage request, CancellationToken ct);
}
