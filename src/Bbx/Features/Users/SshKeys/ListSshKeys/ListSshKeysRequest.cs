namespace Bbx.Features.Users.SshKeys.ListSshKeys;

public sealed record ListSshKeysRequest(string SelectedUser, int Limit);
