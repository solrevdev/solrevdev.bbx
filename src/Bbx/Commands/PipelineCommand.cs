using System.CommandLine;
using Bbx.Features.Pipelines.AddPipelineVariable;
using Bbx.Features.Pipelines.ClearPipelineCache;
using Bbx.Features.Pipelines.CreatePipelineSchedule;
using Bbx.Features.Pipelines.DeletePipelineSchedule;
using Bbx.Features.Pipelines.DeletePipelineVariable;
using Bbx.Features.Pipelines.ListDeploymentEnvironments;
using Bbx.Features.Pipelines.ListPipelineCaches;
using Bbx.Features.Pipelines.ListPipelineSchedules;
using Bbx.Features.Pipelines.ListPipelineSteps;
using Bbx.Features.Pipelines.ListPipelineVariables;
using Bbx.Features.Pipelines.ListPipelines;
using Bbx.Features.Pipelines.PipelineLogs;
using Bbx.Features.Pipelines.StopPipeline;
using Bbx.Features.Pipelines.TriggerPipeline;
using Bbx.Features.Pipelines.ViewDeploymentEnvironment;
using Bbx.Features.Pipelines.ViewPipeline;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Commands;

public static class PipelineCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("pipeline", "Manage Bitbucket Pipelines");

        command.AddCommand(CreateListCommand(services));
        command.AddCommand(CreateViewCommand(services));
        command.AddCommand(CreateTriggerCommand(services));
        command.AddCommand(CreateStopCommand(services));
        command.AddCommand(CreateLogsCommand(services));
        command.AddCommand(CreateStepsCommand(services));
        command.AddCommand(CreateVariablesCommand(services));
        command.AddCommand(CreateSchedulesCommand(services));
        command.AddCommand(CreateCachesCommand(services));
        command.AddCommand(CreateDeploymentsCommand(services));

        return command;
    }

    private static Command CreateListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List pipelines for a repository");
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var statusOption = new Option<string?>("--status", "Filter by status (PENDING, BUILDING, COMPLETED, HALTED, FAILED, SUCCESSFUL, STOPPED)");
        var sortOption = new Option<string>("--sort", () => "-created_on", "Sort field (prefix with - for descending)");
        var limitOption = new Option<int>("--limit", () => 25, "Maximum number of pipelines to return");
        var targetBranchOption = new Option<string?>("--branch", "Filter by target branch");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddOption(statusOption);
        command.AddOption(sortOption);
        command.AddOption(limitOption);
        command.AddOption(targetBranchOption);

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
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var pipelineArg = new Argument<string>("pipeline-uuid", "Pipeline UUID");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddArgument(pipelineArg);

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
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var branchOption = new Option<string>("--branch", () => "main", "Branch to run pipeline on");
        var commitOption = new Option<string?>("--commit", "Specific commit hash to run on");
        var patternOption = new Option<string?>("--pattern", "Custom pipeline pattern to run");
        var variablesOption = new Option<string[]>("--variable", "Pipeline variables in key=value format") { AllowMultipleArgumentsPerToken = true };

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddOption(branchOption);
        command.AddOption(commitOption);
        command.AddOption(patternOption);
        command.AddOption(variablesOption);

        command.SetHandler((string? workspace, string? repo, string branch, string? commit, string? pattern, string[] variables) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<TriggerPipelineHandler>()
                    .HandleAsync(new TriggerPipelineRequest(workspace, repo, branch, commit, pattern, variables), CancellationToken.None)),
            workspaceOption, repoOption, branchOption, commitOption, patternOption, variablesOption);
        return command;
    }

    private static Command CreateStopCommand(IServiceProvider services)
    {
        var command = new Command("stop", "Stop a running pipeline");
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var pipelineArg = new Argument<string>("pipeline-uuid", "Pipeline UUID to stop");
        var yesOption = new Option<bool>("--yes", "Skip confirmation prompt");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddArgument(pipelineArg);
        command.AddOption(yesOption);

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
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var pipelineArg = new Argument<string>("pipeline-uuid", "Pipeline UUID");
        var stepArg = new Argument<string>("step-uuid", "Step UUID");
        var followOption = new Option<bool>("--follow", "Follow log output (not yet implemented)");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddArgument(pipelineArg);
        command.AddArgument(stepArg);
        command.AddOption(followOption);

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
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var pipelineArg = new Argument<string>("pipeline-uuid", "Pipeline UUID");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddArgument(pipelineArg);

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
        command.AddCommand(CreateVariablesListCommand(services));
        command.AddCommand(CreateVariablesAddCommand(services));
        command.AddCommand(CreateVariablesDeleteCommand(services));
        return command;
    }

    private static Command CreateVariablesListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List pipeline variables");
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
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
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var keyOption = new Option<string>("--key", "Variable key") { IsRequired = true };
        var valueOption = new Option<string>("--value", "Variable value") { IsRequired = true };
        var securedOption = new Option<bool>("--secured", "Mark as secured (value hidden)");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddOption(keyOption);
        command.AddOption(valueOption);
        command.AddOption(securedOption);

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
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var uuidArg = new Argument<string>("uuid", "Variable UUID to delete");
        var yesOption = new Option<bool>("--yes", "Skip confirmation prompt");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddArgument(uuidArg);
        command.AddOption(yesOption);

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
        command.AddCommand(CreateSchedulesListCommand(services));
        command.AddCommand(CreateSchedulesCreateCommand(services));
        command.AddCommand(CreateSchedulesDeleteCommand(services));
        return command;
    }

    private static Command CreateSchedulesListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List pipeline schedules");
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
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
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var cronOption = new Option<string>("--cron", "Cron expression (e.g., '0 0 * * *')") { IsRequired = true };
        var branchOption = new Option<string>("--branch", () => "main", "Target branch");
        var patternOption = new Option<string?>("--pattern", "Custom pipeline pattern");
        var enabledOption = new Option<bool>("--enabled", () => true, "Enable the schedule");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddOption(cronOption);
        command.AddOption(branchOption);
        command.AddOption(patternOption);
        command.AddOption(enabledOption);

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
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var uuidArg = new Argument<string>("uuid", "Schedule UUID to delete");
        var yesOption = new Option<bool>("--yes", "Skip confirmation prompt");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddArgument(uuidArg);
        command.AddOption(yesOption);

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
        command.AddCommand(CreateCachesListCommand(services));
        command.AddCommand(CreateCachesClearCommand(services));
        return command;
    }

    private static Command CreateCachesListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List pipeline caches");
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
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
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var nameArg = new Argument<string>("name", "Cache name to clear");
        var yesOption = new Option<bool>("--yes", "Skip confirmation prompt");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddArgument(nameArg);
        command.AddOption(yesOption);

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
        command.AddCommand(CreateDeploymentsListCommand(services));
        command.AddCommand(CreateDeploymentsViewCommand(services));
        return command;
    }

    private static Command CreateDeploymentsListCommand(IServiceProvider services)
    {
        var command = new Command("list", "List deployment environments");
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
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
        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var envArg = new Argument<string>("environment", "Environment UUID or name");
        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddArgument(envArg);
        command.SetHandler((string? workspace, string? repo, string environment) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewDeploymentEnvironmentHandler>()
                    .HandleAsync(new ViewDeploymentEnvironmentRequest(workspace, repo, environment), CancellationToken.None)),
            workspaceOption, repoOption, envArg);
        return command;
    }
}
