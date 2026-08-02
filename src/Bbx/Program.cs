using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
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

        // A value returned from Main overrides Environment.ExitCode, and
        // InvokeAsync reports 0 whenever a handler returned normally. Handlers
        // catch their own errors and set Environment.ExitCode, so returning
        // InvokeAsync's result alone made every failed command exit 0 and look
        // successful to a script or an agent.
        // Several handlers have no try/catch of their own, and the default
        // pipeline answers an escaped exception with a full stack trace. Report
        // the message instead; the exit code stays 1 either way.
        var parser = new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .UseExceptionHandler((exception, context) =>
            {
                Console.Error.WriteLine($"Error: {exception.Message}");
                context.ExitCode = 1;
            })
            .Build();

        var exitCode = await parser.InvokeAsync(args);
        return exitCode != 0 ? exitCode : Environment.ExitCode;
    }
}
