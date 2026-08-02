using System.CommandLine;
using Bbx.Features.Pipelines.AddPipelineVariable;
using Bbx.Features.Pipelines.ClearPipelineCache;
using Bbx.Features.Pipelines.CreatePipelineSchedule;
using Bbx.Features.Pipelines.DeletePipelineSchedule;
using Bbx.Features.Pipelines.DeletePipelineVariable;
using Bbx.Features.Pipelines.ListDeploymentEnvironments;
using Bbx.Features.Pipelines.ListPipelineCaches;
using Bbx.Features.Pipelines.ListPipelineReports;
using Bbx.Features.Pipelines.ListPipelineSchedules;
using Bbx.Features.Pipelines.ListPipelineSteps;
using Bbx.Features.Pipelines.ListPipelineVariables;
using Bbx.Features.Pipelines.ListPipelines;
using Bbx.Features.Pipelines.ListReportAnnotations;
using Bbx.Features.Pipelines.ListTestCases;
using Bbx.Features.Pipelines.ListTestReports;
using Bbx.Features.Pipelines.OidcConfig;
using Bbx.Features.Pipelines.OidcKeys;
using Bbx.Features.Pipelines.PipelineLogs;
using Bbx.Features.Pipelines.StopPipeline;
using Bbx.Features.Pipelines.TriggerPipeline;
using Bbx.Features.Pipelines.ViewDeploymentEnvironment;
using Bbx.Features.Pipelines.ViewPipeline;
using Bbx.Features.Pipelines.ViewPipelineReport;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class PipelineCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("pipeline", "Manage Bitbucket Pipelines");

        command.Subcommands.Add(CreateListCommand(services));
        command.Subcommands.Add(CreateViewCommand(services));
        command.Subcommands.Add(CreateTriggerCommand(services));
        command.Subcommands.Add(CreateStopCommand(services));
        command.Subcommands.Add(CreateLogsCommand(services));
        command.Subcommands.Add(CreateStepsCommand(services));
        command.Subcommands.Add(CreateVariablesCommand(services));
        command.Subcommands.Add(CreateSchedulesCommand(services));
        command.Subcommands.Add(CreateCachesCommand(services));
        command.Subcommands.Add(CreateDeploymentsCommand(services));
        command.Subcommands.Add(CreateReportsCommand(services));
        command.Subcommands.Add(CreateTestReportsCommand(services));
        command.Subcommands.Add(CreateTestCasesCommand(services));
        command.Subcommands.Add(CreateOidcCommand(services));

        return command;
    }

    private static Command CreateReportsCommand(IServiceProvider services)
    {
        var reportsCommand = new Command("reports", "Pipeline report metadata stored against a commit");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        reportsCommand.AddRecursiveOption(workspaceOption);
        reportsCommand.AddRecursiveOption(repoOption);

        var listCommand = new Command("list", "List reports on a commit");
        var listHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var listLimitOption = new Option<int>("--limit") { Description = "Maximum reports to list", DefaultValueFactory = _ => 25 };
        listCommand.Arguments.Add(listHashArg);
        listCommand.Options.Add(listLimitOption);
        listCommand.SetHandler((string? workspace, string? repo, string hash, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListPipelineReportsHandler>()
                    .HandleAsync(new ListPipelineReportsRequest(workspace, repo, hash, limit), CancellationToken.None)),
            workspaceOption, repoOption, listHashArg, listLimitOption);
        reportsCommand.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View a single report on a commit");
        var viewHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var viewReportIdArg = new Argument<string>("report-id") { Description = "Report UUID or external ID" };
        viewCommand.Arguments.Add(viewHashArg);
        viewCommand.Arguments.Add(viewReportIdArg);
        viewCommand.SetHandler((string? workspace, string? repo, string hash, string reportId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewPipelineReportHandler>()
                    .HandleAsync(new ViewPipelineReportRequest(workspace, repo, hash, reportId), CancellationToken.None)),
            workspaceOption, repoOption, viewHashArg, viewReportIdArg);
        reportsCommand.Subcommands.Add(viewCommand);

        var annotationsCommand = new Command("annotations", "List annotations on a report");
        var annHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var annReportIdArg = new Argument<string>("report-id") { Description = "Report UUID or external ID" };
        var annLimitOption = new Option<int>("--limit") { Description = "Maximum annotations to list", DefaultValueFactory = _ => 100 };
        annotationsCommand.Arguments.Add(annHashArg);
        annotationsCommand.Arguments.Add(annReportIdArg);
        annotationsCommand.Options.Add(annLimitOption);
        annotationsCommand.SetHandler((string? workspace, string? repo, string hash, string reportId, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListReportAnnotationsHandler>()
                    .HandleAsync(new ListReportAnnotationsRequest(workspace, repo, hash, reportId, limit), CancellationToken.None)),
            workspaceOption, repoOption, annHashArg, annReportIdArg, annLimitOption);
        reportsCommand.Subcommands.Add(annotationsCommand);

        return reportsCommand;
    }

    private static Command CreateTestReportsCommand(IServiceProvider services)
    {
        var command = new Command("test-reports",
            "Show the test report metadata for a pipeline step");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var pipelineUuidArg = new Argument<string>("pipeline-uuid") { Description = "Pipeline UUID" };
        var stepUuidArg = new Argument<string>("step-uuid") { Description = "Step UUID" };
        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Arguments.Add(pipelineUuidArg);
        command.Arguments.Add(stepUuidArg);
        command.SetHandler((string? workspace, string? repo, string pipelineUuid, string stepUuid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListTestReportsHandler>()
                    .HandleAsync(new ListTestReportsRequest(workspace, repo, pipelineUuid, stepUuid), CancellationToken.None)),
            workspaceOption, repoOption, pipelineUuidArg, stepUuidArg);
        return command;
    }

    private static Command CreateTestCasesCommand(IServiceProvider services)
    {
        var command = new Command("test-cases",
            "List test cases for a pipeline step's test report");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var pipelineUuidArg = new Argument<string>("pipeline-uuid") { Description = "Pipeline UUID" };
        var stepUuidArg = new Argument<string>("step-uuid") { Description = "Step UUID" };
        var limitOption = new Option<int>("--limit") { Description = "Maximum test cases to list", DefaultValueFactory = _ => 200 };
        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Arguments.Add(pipelineUuidArg);
        command.Arguments.Add(stepUuidArg);
        command.Options.Add(limitOption);
        command.SetHandler((string? workspace, string? repo, string pipelineUuid, string stepUuid, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListTestCasesHandler>()
                    .HandleAsync(new ListTestCasesRequest(workspace, repo, pipelineUuid, stepUuid, limit), CancellationToken.None)),
            workspaceOption, repoOption, pipelineUuidArg, stepUuidArg, limitOption);
        return command;
    }

    private static Command CreateOidcCommand(IServiceProvider services)
    {
        var oidcCommand = new Command("oidc", "Pipelines OIDC discovery and JWKS");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        oidcCommand.AddRecursiveOption(workspaceOption);
        oidcCommand.AddRecursiveOption(repoOption);

        var configCommand = new Command("config", "Show the OpenID provider configuration");
        configCommand.SetHandler((string? workspace, string? repo) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<OidcConfigHandler>()
                    .HandleAsync(new OidcConfigRequest(workspace, repo), CancellationToken.None)),
            workspaceOption, repoOption);
        oidcCommand.Subcommands.Add(configCommand);

        var keysCommand = new Command("keys", "Show the JWKS used to verify pipeline OIDC tokens");
        keysCommand.SetHandler((string? workspace, string? repo) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<OidcKeysHandler>()
                    .HandleAsync(new OidcKeysRequest(workspace, repo), CancellationToken.None)),
            workspaceOption, repoOption);
        oidcCommand.Subcommands.Add(keysCommand);

        return oidcCommand;
    }

    private static Command CreateListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List pipelines for a repository");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var statusOption = new Option<string?>("--status") { Description = "Filter by status (PENDING, BUILDING, COMPLETED, HALTED, FAILED, SUCCESSFUL, STOPPED)" };
        var sortOption = new Option<string>("--sort") { Description = "Sort field (prefix with - for descending)", DefaultValueFactory = _ => "-created_on" };
        var limitOption = new Option<int>("--limit") { Description = "Maximum number of pipelines to return", DefaultValueFactory = _ => 25 };
        var targetBranchOption = new Option<string?>("--branch") { Description = "Filter by target branch" };

        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Options.Add(statusOption);
        command.Options.Add(sortOption);
        command.Options.Add(limitOption);
        command.Options.Add(targetBranchOption);

        command.SetHandler((string? workspace, string? repo, string? status, string sort, int limit, string? branch) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListPipelinesHandler>()
                    .HandleAsync(new ListPipelinesRequest(workspace, repo, status, sort, limit, branch), CancellationToken.None)),
            workspaceOption, repoOption, statusOption, sortOption, limitOption, targetBranchOption);
        return command;
    }

    private static Command CreateViewCommand(IServiceProvider services)
    {
        var command = new Command("view", "View details of a specific pipeline");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var pipelineArg = new Argument<string>("pipeline-uuid") { Description = "Pipeline UUID" };

        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Arguments.Add(pipelineArg);

        command.SetHandler((string? workspace, string? repo, string pipelineUuid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewPipelineHandler>()
                    .HandleAsync(new ViewPipelineRequest(workspace, repo, pipelineUuid), CancellationToken.None)),
            workspaceOption, repoOption, pipelineArg);
        return command;
    }

    private static Command CreateTriggerCommand(IServiceProvider services)
    {
        var command = new Command("trigger", "Trigger a new pipeline run");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var branchOption = new Option<string>("--branch") { Description = "Branch to run pipeline on (also used as the PR source branch when --pull-request is set)", DefaultValueFactory = _ => "main" };
        var commitOption = new Option<string?>("--commit") { Description = "Specific commit hash to run on (branch trigger only)" };
        var patternOption = new Option<string?>("--pattern") { Description = "Custom pipeline pattern (selector) to run" };
        var pullRequestOption = new Option<string?>("--pull-request") { Description = "Trigger the pull-request pipeline for this PR id (uses --branch as the PR's source branch)" };
        var variablesOption = new Option<string[]>("--variable") { Description = "Pipeline variables in key=value format" , AllowMultipleArgumentsPerToken = true };

        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Options.Add(branchOption);
        command.Options.Add(commitOption);
        command.Options.Add(patternOption);
        command.Options.Add(pullRequestOption);
        command.Options.Add(variablesOption);

        command.SetHandler((string? workspace, string? repo, string branch, string? commit, string? pattern, string? pullRequest, string[] variables) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<TriggerPipelineHandler>()
                    .HandleAsync(new TriggerPipelineRequest(workspace, repo, branch, commit, pattern, pullRequest, variables), CancellationToken.None)),
            workspaceOption, repoOption, branchOption, commitOption, patternOption, pullRequestOption, variablesOption);
        return command;
    }

    private static Command CreateStopCommand(IServiceProvider services)
    {
        var command = new Command("stop", "Stop a running pipeline");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var pipelineArg = new Argument<string>("pipeline-uuid") { Description = "Pipeline UUID to stop" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation prompt" };

        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Arguments.Add(pipelineArg);
        command.Options.Add(yesOption);

        command.SetHandler(async (string? workspace, string? repo, string pipelineUuid, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Stop pipeline {pipelineUuid}? [y/N]: "))
                return;
            await CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<StopPipelineHandler>()
                    .HandleAsync(new StopPipelineRequest(workspace, repo, pipelineUuid), CancellationToken.None));
        }, workspaceOption, repoOption, pipelineArg, yesOption);
        return command;
    }

    private static Command CreateLogsCommand(IServiceProvider services)
    {
        var command = new Command("logs", "View logs for a pipeline step");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var pipelineArg = new Argument<string>("pipeline-uuid") { Description = "Pipeline UUID" };
        var stepArg = new Argument<string>("step-uuid") { Description = "Step UUID" };
        var followOption = new Option<bool>("--follow") { Description = "Follow log output (not yet implemented)" };

        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Arguments.Add(pipelineArg);
        command.Arguments.Add(stepArg);
        command.Options.Add(followOption);

        command.SetHandler((string? workspace, string? repo, string pipelineUuid, string stepUuid, bool follow) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<PipelineLogsHandler>()
                    .HandleAsync(new PipelineLogsRequest(workspace, repo, pipelineUuid, stepUuid), CancellationToken.None)),
            workspaceOption, repoOption, pipelineArg, stepArg, followOption);
        return command;
    }

    private static Command CreateStepsCommand(IServiceProvider services)
    {
        var command = new Command("steps", "List steps for a pipeline");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var pipelineArg = new Argument<string>("pipeline-uuid") { Description = "Pipeline UUID" };

        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Arguments.Add(pipelineArg);

        command.SetHandler((string? workspace, string? repo, string pipelineUuid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListPipelineStepsHandler>()
                    .HandleAsync(new ListPipelineStepsRequest(workspace, repo, pipelineUuid), CancellationToken.None)),
            workspaceOption, repoOption, pipelineArg);
        return command;
    }

    private static Command CreateVariablesCommand(IServiceProvider services)
    {
        var command = new Command("variables", "Manage pipeline variables");
        command.Subcommands.Add(CreateVariablesListCommand(services));
        command.Subcommands.Add(CreateVariablesAddCommand(services));
        command.Subcommands.Add(CreateVariablesDeleteCommand(services));
        return command;
    }

    private static Command CreateVariablesListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List pipeline variables");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.SetHandler((string? workspace, string? repo) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListPipelineVariablesHandler>()
                    .HandleAsync(new ListPipelineVariablesRequest(workspace, repo), CancellationToken.None)),
            workspaceOption, repoOption);
        return command;
    }

    private static Command CreateVariablesAddCommand(IServiceProvider services)
    {
        var command = new Command("add", "Add a pipeline variable");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var keyOption = new Option<string>("--key") { Description = "Variable key" , Required = true };
        var valueOption = new Option<string>("--value") { Description = "Variable value" , Required = true };
        var securedOption = new Option<bool>("--secured") { Description = "Mark as secured (value hidden)" };

        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Options.Add(keyOption);
        command.Options.Add(valueOption);
        command.Options.Add(securedOption);

        command.SetHandler((string? workspace, string? repo, string key, string value, bool secured) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddPipelineVariableHandler>()
                    .HandleAsync(new AddPipelineVariableRequest(workspace, repo, key, value, secured), CancellationToken.None)),
            workspaceOption, repoOption, keyOption, valueOption, securedOption);
        return command;
    }

    private static Command CreateVariablesDeleteCommand(IServiceProvider services)
    {
        var command = new Command("delete", "Delete a pipeline variable");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var uuidArg = new Argument<string>("uuid") { Description = "Variable UUID to delete" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation prompt" };

        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Arguments.Add(uuidArg);
        command.Options.Add(yesOption);

        command.SetHandler(async (string? workspace, string? repo, string uuid, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete variable {uuid}? [y/N]: "))
                return;
            await CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<DeletePipelineVariableHandler>()
                    .HandleAsync(new DeletePipelineVariableRequest(workspace, repo, uuid), CancellationToken.None));
        }, workspaceOption, repoOption, uuidArg, yesOption);
        return command;
    }

    private static Command CreateSchedulesCommand(IServiceProvider services)
    {
        var command = new Command("schedules", "Manage pipeline schedules");
        command.Subcommands.Add(CreateSchedulesListCommand(services));
        command.Subcommands.Add(CreateSchedulesCreateCommand(services));
        command.Subcommands.Add(CreateSchedulesDeleteCommand(services));
        return command;
    }

    private static Command CreateSchedulesListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List pipeline schedules");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.SetHandler((string? workspace, string? repo) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListPipelineSchedulesHandler>()
                    .HandleAsync(new ListPipelineSchedulesRequest(workspace, repo), CancellationToken.None)),
            workspaceOption, repoOption);
        return command;
    }

    private static Command CreateSchedulesCreateCommand(IServiceProvider services)
    {
        var command = new Command("create", "Create a pipeline schedule");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var cronOption = new Option<string>("--cron") { Description = "Cron expression (e.g., '0 0 * * *')" , Required = true };
        var branchOption = new Option<string>("--branch") { Description = "Target branch", DefaultValueFactory = _ => "main" };
        var patternOption = new Option<string?>("--pattern") { Description = "Custom pipeline pattern" };
        var enabledOption = new Option<bool>("--enabled") { Description = "Enable the schedule", DefaultValueFactory = _ => true };

        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Options.Add(cronOption);
        command.Options.Add(branchOption);
        command.Options.Add(patternOption);
        command.Options.Add(enabledOption);

        command.SetHandler((string? workspace, string? repo, string cron, string branch, string? pattern, bool enabled) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreatePipelineScheduleHandler>()
                    .HandleAsync(new CreatePipelineScheduleRequest(workspace, repo, cron, branch, pattern, enabled), CancellationToken.None)),
            workspaceOption, repoOption, cronOption, branchOption, patternOption, enabledOption);
        return command;
    }

    private static Command CreateSchedulesDeleteCommand(IServiceProvider services)
    {
        var command = new Command("delete", "Delete a pipeline schedule");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var uuidArg = new Argument<string>("uuid") { Description = "Schedule UUID to delete" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation prompt" };

        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Arguments.Add(uuidArg);
        command.Options.Add(yesOption);

        command.SetHandler(async (string? workspace, string? repo, string uuid, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete schedule {uuid}? [y/N]: "))
                return;
            await CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<DeletePipelineScheduleHandler>()
                    .HandleAsync(new DeletePipelineScheduleRequest(workspace, repo, uuid), CancellationToken.None));
        }, workspaceOption, repoOption, uuidArg, yesOption);
        return command;
    }

    private static Command CreateCachesCommand(IServiceProvider services)
    {
        var command = new Command("caches", "Manage pipeline caches");
        command.Subcommands.Add(CreateCachesListCommand(services));
        command.Subcommands.Add(CreateCachesClearCommand(services));
        return command;
    }

    private static Command CreateCachesListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List pipeline caches");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.SetHandler((string? workspace, string? repo) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListPipelineCachesHandler>()
                    .HandleAsync(new ListPipelineCachesRequest(workspace, repo), CancellationToken.None)),
            workspaceOption, repoOption);
        return command;
    }

    private static Command CreateCachesClearCommand(IServiceProvider services)
    {
        var command = new Command("clear", "Clear a pipeline cache");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var nameArg = new Argument<string>("name") { Description = "Cache name to clear" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation prompt" };

        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Arguments.Add(nameArg);
        command.Options.Add(yesOption);

        command.SetHandler(async (string? workspace, string? repo, string name, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Clear cache '{name}'? [y/N]: "))
                return;
            await CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ClearPipelineCacheHandler>()
                    .HandleAsync(new ClearPipelineCacheRequest(workspace, repo, name), CancellationToken.None));
        }, workspaceOption, repoOption, nameArg, yesOption);
        return command;
    }

    private static Command CreateDeploymentsCommand(IServiceProvider services)
    {
        var command = new Command("deployments", "Manage deployment environments");
        command.Subcommands.Add(CreateDeploymentsListCommand(services));
        command.Subcommands.Add(CreateDeploymentsViewCommand(services));
        return command;
    }

    private static Command CreateDeploymentsListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List deployment environments");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.SetHandler((string? workspace, string? repo) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListDeploymentEnvironmentsHandler>()
                    .HandleAsync(new ListDeploymentEnvironmentsRequest(workspace, repo), CancellationToken.None)),
            workspaceOption, repoOption);
        return command;
    }

    private static Command CreateDeploymentsViewCommand(IServiceProvider services)
    {
        var command = new Command("view", "View deployment environment details");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var envArg = new Argument<string>("environment") { Description = "Environment UUID or name" };
        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Arguments.Add(envArg);
        command.SetHandler((string? workspace, string? repo, string environment) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewDeploymentEnvironmentHandler>()
                    .HandleAsync(new ViewDeploymentEnvironmentRequest(workspace, repo, environment), CancellationToken.None)),
            workspaceOption, repoOption, envArg);
        return command;
    }
}
