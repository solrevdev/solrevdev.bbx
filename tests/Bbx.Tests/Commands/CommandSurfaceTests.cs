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

    // The lock cannot be changed through this endpoint, so --lock and --unlock
    // were dropped for --admin-only and --no-admin-only. A stale flag would
    // still compile, so the parse is asserted here.
    [Fact]
    public void Environment_changes_takes_admin_only_and_no_longer_takes_lock()
    {
        var command = PipelineCommand.Create(Services());

        Parse(command, "deployments", "changes", "e1", "-r", "widgets", "--admin-only")
            .Errors.Should().BeEmpty();
        Parse(command, "deployments", "changes", "e1", "-r", "widgets", "--no-admin-only")
            .Errors.Should().BeEmpty();
        Parse(command, "deployments", "changes", "e1", "-r", "widgets", "--lock")
            .Errors.Should().NotBeEmpty();
        Parse(command, "deployments", "changes", "e1", "-r", "widgets", "--reason", "freeze")
            .Errors.Should().NotBeEmpty();
    }

    // Only the workspace form of the OIDC path exists. The repository form
    // answered 404 "There is no API hosted at this URL", so the command is gone.
    [Fact]
    public void Pipeline_has_no_repository_scoped_oidc_command()
    {
        Parse(PipelineCommand.Create(Services()), "oidc", "config", "-r", "widgets")
            .Errors.Should().NotBeEmpty();
        Parse(WorkspaceCommand.Create(Services()), "pipelines", "oidc", "config", "-w", "acme")
            .Errors.Should().BeEmpty();
    }

    // PUT deploy-keys/{key_id} cannot succeed, so there is no update verb.
    [Fact]
    public void Deploy_keys_has_no_update_verb()
    {
        Parse(RepoCommand.Create(Services()),
                "deploy-keys", "update", "7", "-r", "widgets", "--key", "ssh-ed25519 AAAA")
            .Errors.Should().NotBeEmpty();
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
    public void Pr_update_takes_every_mutable_field()
    {
        var result = Parse(PrCommand.Create(Services()),
            "update", "1", "--title", "New", "--body", "Text", "--dest", "main",
            "--reviewers", "557058:abc", "-r", "widgets");

        result.Errors.Should().BeEmpty();
        result.GetValue<string>("--title").Should().Be("New");
        result.GetValue<string>("--body").Should().Be("Text");
        result.GetValue<string>("--dest").Should().Be("main");
        result.GetValue<string[]>("--reviewers").Should().Equal("557058:abc");
    }

    // Clearing a description means sending "". If the parser folded an empty
    // value into null the handler would drop the field and the clear would
    // silently do nothing.
    [Fact]
    public void Pr_update_keeps_an_empty_body_distinct_from_an_absent_one()
    {
        var command = PrCommand.Create(Services());

        Parse(command, "update", "1", "--body", "").GetValue<string>("--body").Should().BeEmpty();
        Parse(command, "update", "1", "--title", "t").GetValue<string>("--body").Should().BeNull();
    }

    // pr update has no required option, so a bare "pr update 1" parses. The
    // handler is what rejects it; this pins the parse half of that contract.
    [Fact]
    public void Pr_update_parses_with_no_fields_and_leaves_them_unset()
    {
        var result = Parse(PrCommand.Create(Services()), "update", "1");

        result.Errors.Should().BeEmpty();
        result.GetValue<string>("--title").Should().BeNull();
        BoolValue(result, "--close-source-branch").Should().BeFalse();
        BoolValue(result, "--no-close-source-branch").Should().BeFalse();
    }

    // An absent array option parses to an empty array, not null. A handler that
    // tests the array for null therefore treats "no --reviewers" as "replace the
    // reviewers with nobody", which quietly strips them on every unrelated edit.
    [Fact]
    public void An_absent_array_option_parses_to_an_empty_array()
    {
        var result = Parse(PrCommand.Create(Services()), "update", "1", "--title", "t");

        result.GetValue<string[]>("--reviewers").Should().BeEmpty();
    }

    [Fact]
    public void Pr_update_accepts_both_source_branch_flags_at_parse_time()
    {
        var result = Parse(PrCommand.Create(Services()),
            "update", "1", "--close-source-branch", "--no-close-source-branch");

        result.Errors.Should().BeEmpty();
        BoolValue(result, "--close-source-branch").Should().BeTrue();
        BoolValue(result, "--no-close-source-branch").Should().BeTrue();
    }

    // repo update carries fifteen bound symbols, more than SetHandler takes, so
    // it reads the parse result itself. That hand-rolled binding is exactly the
    // kind that compiles while doing nothing.
    [Fact]
    public void Repo_update_binds_its_options()
    {
        var result = Parse(RepoCommand.Create(Services()),
            "update", "myrepo", "--description", "d", "--main-branch", "trunk", "--public");

        result.Errors.Should().BeEmpty();
        result.GetValue<string>("--description").Should().Be("d");
        result.GetValue<string>("--main-branch").Should().Be("trunk");
        BoolValue(result, "--public").Should().BeTrue();
        BoolValue(result, "--private").Should().BeFalse();
    }

    [Fact]
    public void Repo_update_parses_with_no_fields_and_leaves_them_unset()
    {
        var result = Parse(RepoCommand.Create(Services()), "update", "myrepo");

        result.Errors.Should().BeEmpty();
        result.GetValue<string>("--name").Should().BeNull();
        BoolValue(result, "--issues").Should().BeFalse();
        BoolValue(result, "--no-issues").Should().BeFalse();
    }

    // src ls used to require --ref. Omitting it now lists the root of the main
    // branch, so the option must not have crept back to Required.
    [Fact]
    public void Src_ls_parses_without_a_ref()
    {
        var result = Parse(SrcCommand.Create(Services()), "ls", "-r", "myrepo");

        result.Errors.Should().BeEmpty();
        result.GetValue<string>("--ref").Should().BeNull();
    }

    // Same hazard as --reviewers: these replace the exemption list on a branch
    // restriction, so an empty array must stay distinguishable from a request
    // to clear it.
    [Fact]
    public void Branch_restriction_update_array_options_are_repeatable_and_empty_when_absent()
    {
        var command = BranchCommand.Create(Services());

        Parse(command, "restrictions", "update", "1", "--pattern", "main")
            .GetValue<string[]>("--users").Should().BeEmpty();

        var given = Parse(command, "restrictions", "update", "1", "--users", "{a}", "--users", "{b}");
        given.Errors.Should().BeEmpty();
        given.GetValue<string[]>("--users").Should().Equal("{a}", "{b}");
    }

    // pr activity took a required id. It now covers the repository-wide feed as
    // well, which only works if the argument really is optional at parse time.
    [Fact]
    public void Pr_activity_parses_with_and_without_an_id()
    {
        var command = PrCommand.Create(Services());

        var repoWide = Parse(command, "activity", "-r", "myrepo");
        repoWide.Errors.Should().BeEmpty();
        repoWide.GetValue<int?>("id").Should().BeNull();

        var single = Parse(command, "activity", "7", "-r", "myrepo");
        single.Errors.Should().BeEmpty();
        single.GetValue<int?>("id").Should().Be(7);
    }

    // A recursive option only binds when the group it belongs to registers it.
    // Missing that is what sent every default-reviewers call to
    // /workspaces/{ws}/projects//default-reviewers.
    [Fact]
    public void Project_access_binds_the_project_key()
    {
        var result = Parse(WorkspaceCommand.Create(Services()),
            "project", "access", "groups", "list", "--project-key", "PROJ");

        result.Errors.Should().BeEmpty();
        result.GetValue<string>("--project-key").Should().Be("PROJ");
    }

    [Fact]
    public void Repo_access_remove_defaults_to_prompting()
    {
        var result = Parse(RepoCommand.Create(Services()),
            "access", "users", "remove", "557058:abc", "-r", "myrepo");

        result.Errors.Should().BeEmpty();
        BoolValue(result, "--yes").Should().BeFalse();
    }

    // caches clear took a required argument. Clearing by name goes to a
    // different endpoint, so the UUID had to become optional.
    [Fact]
    public void Pipeline_caches_clear_parses_with_a_uuid_or_with_a_name()
    {
        var command = PipelineCommand.Create(Services());

        var byUuid = Parse(command, "caches", "clear", "{c1}", "-r", "myrepo");
        byUuid.Errors.Should().BeEmpty();
        byUuid.GetValue<string>("cache-uuid").Should().Be("{c1}");
        byUuid.GetValue<string>("--name").Should().BeNull();

        var byName = Parse(command, "caches", "clear", "--name", "node", "-r", "myrepo");
        byName.Errors.Should().BeEmpty();
        byName.GetValue<string>("cache-uuid").Should().BeNull();
        byName.GetValue<string>("--name").Should().Be("node");
    }

    // Deployment variables are scoped to an environment, and the option is
    // recursive on the group rather than repeated on each verb.
    [Fact]
    public void Deployment_variables_bind_the_environment()
    {
        var result = Parse(PipelineCommand.Create(Services()),
            "deployments", "variables", "list", "-r", "myrepo", "-e", "{env}");

        result.Errors.Should().BeEmpty();
        result.GetValue<string>("--environment").Should().Be("{env}");
    }

    // annotation-update carries eleven bound symbols, past SetHandler's nine,
    // so it reads the parse result itself.
    [Fact]
    public void Report_annotation_update_binds_its_options()
    {
        var result = Parse(PipelineCommand.Create(Services()),
            "reports", "annotation-update", "abc123", "r1", "a1",
            "--summary", "Unused import", "--path", "src/a.cs", "--line", "3");

        result.Errors.Should().BeEmpty();
        result.GetValue<string>("--summary").Should().Be("Unused import");
        result.GetValue<int?>("--line").Should().Be(3);
    }

    [Fact]
    public void Snippet_writes_take_an_optional_revision()
    {
        var command = SnippetCommand.Create(Services());

        var latest = Parse(command, "view", "abc");
        latest.Errors.Should().BeEmpty();
        latest.GetValue<string>("--revision").Should().BeNull();

        var pinned = Parse(command, "update", "abc", "--title", "t", "--revision", "d3adb33f");
        pinned.Errors.Should().BeEmpty();
        pinned.GetValue<string>("--revision").Should().Be("d3adb33f");
    }

    [Fact]
    public void Workspace_pipeline_variables_bind_the_workspace_recursively()
    {
        var result = Parse(WorkspaceCommand.Create(Services()),
            "pipelines", "variables", "list", "-w", "acme");

        result.Errors.Should().BeEmpty();
        result.GetValue<string>("--workspace").Should().Be("acme");
    }

    [Fact]
    public void Project_update_accepts_both_visibility_flags_at_parse_time()
    {
        var result = Parse(WorkspaceCommand.Create(Services()),
            "project", "update", "PROJ", "--private", "--public");

        result.Errors.Should().BeEmpty();
        BoolValue(result, "--private").Should().BeTrue();
        BoolValue(result, "--public").Should().BeTrue();
    }

    [Fact]
    public void The_projects_alias_still_resolves()
    {
        var result = Parse(WorkspaceCommand.Create(Services()), "projects", "list", "-w", "acme");

        result.Errors.Should().BeEmpty();
    }
}
