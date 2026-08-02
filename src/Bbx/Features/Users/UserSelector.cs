using System.Text.Json;
using Bbx.Api;

namespace Bbx.Features.Users;

internal static class UserSelector
{
    /// <summary>
    /// Turn an omitted or "me" selector into the authenticated user's UUID.
    /// </summary>
    /// <remarks>
    /// Bitbucket has no <c>/2.0/users/me</c> alias: that path is read as a
    /// request for the account literally named "me" and answered with
    /// "You cannot administer personal accounts of other users." Usernames were
    /// also withdrawn as selectors, so the account UUID from <c>/2.0/user</c> is
    /// what the <c>/2.0/users/{selected_user}</c> routes actually accept.
    /// </remarks>
    public static async Task<string> ResolveAsync(
        BitbucketClient client, string? selectedUser, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(selectedUser)
            && !selectedUser.Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            return selectedUser;
        }

        var me = await client.GetAsync<JsonElement>("user", ct);
        var id = me.GetStringOrNull("uuid") ?? me.GetStringOrNull("account_id");

        return string.IsNullOrEmpty(id)
            ? throw new BbxUserException("Error: could not resolve the current account from /2.0/user.")
            : id;
    }
}
