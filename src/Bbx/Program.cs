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

        // `--json-compact` is a global formatting toggle. It's stripped here
        // before System.CommandLine sees the args so every group inherits
        // it transparently — same effect as setting BBX_JSON_COMPACT=1.
        var compactEnv = Environment.GetEnvironmentVariable("BBX_JSON_COMPACT");
        var compactFromEnv = !string.IsNullOrEmpty(compactEnv)
            && (compactEnv == "1" || string.Equals(compactEnv, "true", StringComparison.OrdinalIgnoreCase));
        var compactFromArg = args.Any(a => a == "--json-compact");
        JsonOptions.UseCompact = compactFromArg || compactFromEnv;
        if (compactFromArg)
            args = args.Where(a => a != "--json-compact").ToArray();

        Services = ServiceRegistration.Build();

        var rootCommand = new RootCommand("Bitbucket Cloud CLI for LLM integration")
        {
            Name = "bbx",
        };
        // Registered for --help discoverability only; the option is already
        // consumed above before InvokeAsync runs.
        rootCommand.AddGlobalOption(new Option<bool>(
            "--json-compact",
            "Print JSON on a single line (no whitespace). Default: pretty-printed. Env: BBX_JSON_COMPACT=1."));

        rootCommand.AddCommand(AuthCommand.Create(Services));
        rootCommand.AddCommand(RepoCommand.Create(Services));
        rootCommand.AddCommand(PrCommand.Create(Services));
        rootCommand.AddCommand(BranchCommand.Create(Services));
        rootCommand.AddCommand(CommitCommand.Create(Services));
        rootCommand.AddCommand(SrcCommand.Create(Services));
        rootCommand.AddCommand(DownloadCommand.Create(Services));
        rootCommand.AddCommand(IssueCommand.Create(Services));
        rootCommand.AddCommand(PipelineCommand.Create(Services));
        rootCommand.AddCommand(SnippetCommand.Create(Services));
        rootCommand.AddCommand(WorkspaceCommand.Create(Services));
        rootCommand.AddCommand(UserCommand.Create(Services));

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
