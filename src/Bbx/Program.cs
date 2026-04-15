using System.CommandLine;
using System.Text;
using Bbx.Commands;

namespace Bbx;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var rootCommand = new RootCommand("Bitbucket Cloud CLI for LLM integration")
        {
            Name = "bbx"
        };

        // Add all command groups
        rootCommand.AddCommand(AuthCommand.Create());
        rootCommand.AddCommand(RepoCommand.Create());
        rootCommand.AddCommand(PrCommand.Create());
        rootCommand.AddCommand(BranchCommand.Create());
        rootCommand.AddCommand(CommitCommand.Create());
        rootCommand.AddCommand(IssueCommand.Create());
        rootCommand.AddCommand(PipelineCommand.Create());
        rootCommand.AddCommand(SnippetCommand.Create());
        rootCommand.AddCommand(WorkspaceCommand.Create());

        // Version command
        var versionCommand = new Command("version", "Show version information");
        versionCommand.SetHandler(() =>
        {
            var version = typeof(Program).Assembly.GetName().Version;
            Console.WriteLine($"bbx version {version?.ToString(3) ?? "1.0.0"}");
        });
        rootCommand.AddCommand(versionCommand);

        return await rootCommand.InvokeAsync(args);
    }
}
