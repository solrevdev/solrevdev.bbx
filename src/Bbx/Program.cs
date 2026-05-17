using System.CommandLine;
using System.Text;
using Bbx.Commands;
using Bbx.Composition;

namespace Bbx;

public class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Services = ServiceRegistration.Build();

        var rootCommand = new RootCommand("Bitbucket Cloud CLI for LLM integration")
        {
            Name = "bbx",
        };

        rootCommand.AddCommand(AuthCommand.Create(Services));
        rootCommand.AddCommand(RepoCommand.Create(Services));
        rootCommand.AddCommand(PrCommand.Create(Services));
        rootCommand.AddCommand(BranchCommand.Create(Services));
        rootCommand.AddCommand(CommitCommand.Create(Services));
        rootCommand.AddCommand(IssueCommand.Create(Services));
        rootCommand.AddCommand(PipelineCommand.Create(Services));
        rootCommand.AddCommand(SnippetCommand.Create(Services));
        rootCommand.AddCommand(WorkspaceCommand.Create(Services));

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
