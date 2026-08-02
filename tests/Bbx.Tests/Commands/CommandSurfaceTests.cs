using System.CommandLine;
using AwesomeAssertions;
using Bbx.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Tests.Commands;

/// <summary>
/// Parse-level guards on the CLI surface.
/// </summary>
/// <remarks>
/// Added when the project moved from System.CommandLine 2.0.0-beta4 to 2.0.
/// Building the command tree is done entirely in <c>Commands/</c>, so a broken
/// option definition compiles cleanly and only fails when a user runs it.
/// 2.0 also stopped printing <c>[default: True]</c> in help, which means the
/// help text no longer reveals whether a default survived. These assert the
/// parsed value instead.
/// </remarks>
public class CommandSurfaceTests
{
    private static IServiceProvider Services() => new ServiceCollection().BuildServiceProvider();

    private static ParseResult Parse(Command command, params string[] args)
        => command.Parse(args);

    private static bool BoolValue(ParseResult result, string optionName)
    {
        var option = (Option<bool>)result.CommandResult.Command.Options.Single(o => o.Name == optionName);
        return result.GetValue(option);
    }

    // A webhook created with --active silently defaulting to false would be
    // registered but never fire.
    [Theory]
    [InlineData("repo")]
    [InlineData("workspace")]
    public void Webhook_create_defaults_to_active(string group)
    {
        Command command = group == "repo"
            ? RepoCommand.Create(Services())
            : WorkspaceCommand.Create(Services());

        var result = Parse(command, "hooks", "create", "--url", "https://example.com/hook");

        result.Errors.Should().BeEmpty();
        BoolValue(result, "--active").Should().BeTrue();
    }

    [Fact]
    public void Pipeline_schedule_create_defaults_to_enabled()
    {
        var result = Parse(PipelineCommand.Create(Services()),
            "schedules", "create", "--cron", "0 0 1 * * ? *");

        result.Errors.Should().BeEmpty();
        BoolValue(result, "--enabled").Should().BeTrue();
    }

    [Fact]
    public void Confirmation_flags_default_to_false_so_deletes_still_prompt()
    {
        var result = Parse(RepoCommand.Create(Services()), "delete", "myrepo");

        result.Errors.Should().BeEmpty();
        BoolValue(result, "--yes").Should().BeFalse();
    }

    // Every repo-scoped group takes the short forms; pipeline was the one group
    // that only accepted the long ones.
    [Theory]
    [InlineData("pr", "list")]
    [InlineData("branch", "list")]
    [InlineData("commit", "list")]
    [InlineData("pipeline", "list")]
    [InlineData("download", "list")]
    public void Short_workspace_and_repo_aliases_parse(string group, string verb)
    {
        var command = group switch
        {
            "pr" => PrCommand.Create(Services()),
            "branch" => BranchCommand.Create(Services()),
            "commit" => CommitCommand.Create(Services()),
            "pipeline" => PipelineCommand.Create(Services()),
            _ => DownloadCommand.Create(Services()),
        };

        var result = Parse(command, verb, "-w", "acme", "-r", "widgets");

        result.Errors.Should().BeEmpty();
        result.GetValue<string>("--workspace").Should().Be("acme");
        result.GetValue<string>("--repo").Should().Be("widgets");
    }

    // `repo list` covers a whole workspace, so it takes -w but deliberately no -r.
    [Fact]
    public void Repo_list_takes_a_workspace_but_not_a_repo()
    {
        var command = RepoCommand.Create(Services());

        Parse(command, "list", "-w", "acme").Errors.Should().BeEmpty();
        Parse(command, "list", "-r", "widgets").Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void A_missing_required_option_is_a_parse_error()
    {
        var result = Parse(PipelineCommand.Create(Services()), "schedules", "create");

        result.Errors.Should().NotBeEmpty("--cron is required");
    }

    [Fact]
    public void Recursive_options_reach_nested_subcommands()
    {
        var result = Parse(WorkspaceCommand.Create(Services()),
            "project", "deploy-keys", "list", "--project-key", "KEY", "-w", "acme");

        result.Errors.Should().BeEmpty();
        result.GetValue<string>("--project-key").Should().Be("KEY");
        result.GetValue<string>("--workspace").Should().Be("acme");
    }

    [Fact]
    public void Repeated_file_options_collect_into_an_array()
    {
        var result = Parse(SrcCommand.Create(Services()),
            "write", "--branch", "main", "--message", "m", "--file", "a=b", "--file", "c=d");

        result.Errors.Should().BeEmpty();
        result.GetValue<string[]>("--file").Should().Equal("a=b", "c=d");
    }

    [Fact]
    public void Merge_strategy_defaults_to_a_value_bitbucket_accepts()
    {
        var result = Parse(PrCommand.Create(Services()), "merge", "1");

        result.Errors.Should().BeEmpty();
        result.GetValue<string>("--strategy").Should().Be("merge_commit");
    }

    [Fact]
    public void The_projects_alias_still_resolves()
    {
        var result = Parse(WorkspaceCommand.Create(Services()), "projects", "list", "-w", "acme");

        result.Errors.Should().BeEmpty();
    }
}
