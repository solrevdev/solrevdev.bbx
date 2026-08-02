namespace Bbx.Features.Users.SshKeys.DeleteSshKey;

public sealed record DeleteSshKeyRequest(string SelectedUser, string KeyId);
