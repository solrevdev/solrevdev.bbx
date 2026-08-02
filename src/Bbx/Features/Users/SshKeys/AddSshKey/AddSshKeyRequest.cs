namespace Bbx.Features.Users.SshKeys.AddSshKey;

public sealed record AddSshKeyRequest(string SelectedUser, string Key, string? Label);
