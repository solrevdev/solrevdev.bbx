namespace Bbx.Features.Users.GpgKeys.ListGpgKeys;

public sealed record ListGpgKeysRequest(string? SelectedUser, int Limit);
