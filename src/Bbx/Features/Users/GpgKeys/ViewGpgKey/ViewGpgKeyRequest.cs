namespace Bbx.Features.Users.GpgKeys.ViewGpgKey;

public sealed record ViewGpgKeyRequest(string? SelectedUser, string Fingerprint);
