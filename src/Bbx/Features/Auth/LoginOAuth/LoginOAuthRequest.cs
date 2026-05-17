namespace Bbx.Features.Auth.LoginOAuth;

public sealed record LoginOAuthRequest(
    string? ClientId,
    string? ClientSecret,
    int Port,
    bool NoBrowser,
    string? Scopes);
