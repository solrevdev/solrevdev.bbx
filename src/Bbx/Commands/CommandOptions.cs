using System.CommandLine;

namespace Bbx.Commands;

internal static class CommandOptions
{
    public static Option<string?> CreateWorkspaceOption()
    {
        return new Option<string?>(
            aliases: ["--workspace", "-w"],
            description: "Bitbucket workspace (defaults to configured workspace)");
    }

    public static Option<string?> CreateRepoOption()
    {
        return new Option<string?>(
            aliases: ["--repo", "-r"],
            description: "Repository slug");
    }
}
