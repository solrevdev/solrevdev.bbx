using System.CommandLine;

namespace Bbx.Commands;

internal static class CommandOptions
{
    public static Option<string?> CreateWorkspaceOption()
    {
        return new Option<string?>("--workspace", "-w")
        {
            Description = "Bitbucket workspace (defaults to configured workspace)",
        };
    }

    public static Option<string?> CreateRepoOption()
    {
        return new Option<string?>("--repo", "-r")
        {
            Description = "Repository slug",
        };
    }
}
