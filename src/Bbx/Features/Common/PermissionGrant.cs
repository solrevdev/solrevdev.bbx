using System.Text.Json;
using Bbx.Api;

namespace Bbx.Features.Common;

/// <summary>
/// Shared shaping for the permissions-config endpoints, which answer the same
/// grant record whether it is scoped to a repository or a project, and whether
/// it names a user or a group.
/// </summary>
internal static class PermissionGrant
{
    public static void EnsureValid(string permission, IReadOnlyCollection<string> allowed)
    {
        if (!allowed.Contains(permission, StringComparer.OrdinalIgnoreCase))
            throw new BbxUserException(
                $"Error: --permission must be one of {string.Join(", ", allowed)}.");
    }

    public static object Project(JsonElement grant) => new
    {
        type = grant.GetStringOrNull("type"),
        permission = grant.GetStringOrNull("permission"),
        // A grant carries exactly one of these two. The other is absent, not
        // null, but reading either through TryGetProperty would still throw on
        // a repository grant, which sends "repository": null nowhere and
        // "project": null on the project-scoped call.
        user = grant.TryGetObject("user", out var u)
            ? new { display_name = u.GetStringOrNull("display_name"), account_id = u.GetStringOrNull("account_id"), uuid = u.GetStringOrNull("uuid") }
            : null,
        group = grant.TryGetObject("group", out var g)
            ? new { name = g.GetStringOrNull("name"), slug = g.GetStringOrNull("slug") }
            : null,
    };
}
