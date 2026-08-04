using System.CommandLine;
using System.CommandLine.Invocation;
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
        return await RunAsync(args, ServiceRegistration.Build());
    }

    internal static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
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

        Services = services;

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
            exitCode = await parseResult.InvokeAsync(new InvocationConfiguration
            {
                EnableDefaultExceptionHandler = false,
            }, default);
        }
        catch (BbxUserException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            // Do not print a full stack trace for an unexpected CLI failure.
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }

        return exitCode;
    }
}
