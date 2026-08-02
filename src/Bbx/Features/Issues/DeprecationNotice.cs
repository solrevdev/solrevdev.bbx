namespace Bbx.Features.Issues;

internal static class DeprecationNotice
{
    public static void Emit()
    {
        Console.Error.WriteLine("WARNING: Bitbucket Cloud Issues are being sunset by Atlassian.");
        Console.Error.WriteLine("  API endpoints will be removed on August 20, 2026.");
        Console.Error.WriteLine("  Migrate to Jira Software: https://support.atlassian.com/bitbucket-cloud/docs/export-or-import-issue-data/");
        Console.Error.WriteLine("  Full announcement: https://community.atlassian.com/forums/Bitbucket-articles/Announcing-sunset-of-Bitbucket-Issues-and-Wikis/ba-p/3193882");
        Console.Error.WriteLine();
    }
}
