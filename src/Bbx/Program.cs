using System.CommandLine;
using System.Text;
using Bbx.Commands;
using Bbx.Composition;

namespace Bbx;

public class Program
{
    // internal set so tests can substitute a provider; Main is the only writer in production.
    public static IServiceProvider Services { get; internal set; } = null!;

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // `--json-compact` is a global formatting toggle. It's stripped here
        // before System.CommandLine sees the args so every group inherits
        // it transparently, the same effect as setting BBX_JSON_COMPACT=1.
        var compactEnv = Environment.GetEnvironmentVariable("BBX_JSON_COMPACT");
        var compactFromEnv = !string.IsNullOrEmpty(compactEnv)
            && (compactEnv == "1" || string.Equals(compactEnv, "true", StringComparison.OrdinalIgnoreCase));
        var compactFromArg = args.Any(a => a == "--json-compact");
        JsonOptions.UseCompact = compactFromArg || compactFromEnv;
        if (compactFromArg)
            args = args.Where(a => a != "--json-compact").ToArray();

        Services = ServiceRegistration.Build();

        var rootCommand = new RootCommand("Bitbucket Cloud CLI for LLM integration");
        // Registered for --help discoverability only; the option is already
        // consumed above before the parse runs.
        rootCommand.AddRecursiveOption(new Option<bool>("--json-compact")
        {
            Description = "Print JSON on a single line (no whitespace). Default: pretty-printed. Env: BBX_JSON_COMPACT=1.",
        });

        rootCommand.Subcommands.Add(AuthCommand.Create(Services));
        rootCommand.Subcommands.Add(RepoCommand.Create(Services));
        rootCommand.Subcommands.Add(PrCommand.Create(Services));
        rootCommand.Subcommands.Add(BranchCommand.Create(Services));
        rootCommand.Subcommands.Add(CommitCommand.Create(Services));
        rootCommand.Subcommands.Add(SrcCommand.Create(Services));
        rootCommand.Subcommands.Add(DownloadCommand.Create(Services));
        rootCommand.Subcommands.Add(IssueCommand.Create(Services));
        rootCommand.Subcommands.Add(PipelineCommand.Create(Services));
        rootCommand.Subcommands.Add(SnippetCommand.Create(Services));
        rootCommand.Subcommands.Add(WorkspaceCommand.Create(Services));
        rootCommand.Subcommands.Add(UserCommand.Create(Services));

        var versionCommand = new Command("version", "Show version information");
        versionCommand.SetAction(_ =>
        {
            var version = typeof(Program).Assembly.GetName().Version;
            Console.WriteLine($"bbx version {version?.ToString(3) ?? "1.0.0"}");
        });
        rootCommand.Subcommands.Add(versionCommand);

        var parseResult = rootCommand.Parse(args);

        int exitCode;
        try
        {
            exitCode = await parseResult.InvokeAsync();
        }
        catch (Exception ex)
        {
            // CommandRunner catches the expected failures. Anything else that
            // escapes a handler would otherwise print a full stack trace;
            // System.CommandLine 2.0 dropped the built-in exception handler, so
            // report the message here instead.
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }

        // A value returned from Main overrides Environment.ExitCode, and the
        // invocation reports 0 whenever a handler returned normally. Handlers
        // catch their own errors and set Environment.ExitCode, so returning the
        // invocation's result alone made every failed command exit 0 and look
        // successful to a script or an agent.
        return exitCode != 0 ? exitCode : Environment.ExitCode;
    }
}
