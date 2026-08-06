using System.CommandLine;
using Bbx.Features.Pipelines.AddDeploymentVariable;
using Bbx.Features.Pipelines.AddPipelineKnownHost;
using Bbx.Features.Pipelines.ChangeDeploymentEnvironment;
using Bbx.Features.Pipelines.ClearAllPipelineCaches;
using Bbx.Features.Pipelines.CreateDeploymentEnvironment;
using Bbx.Features.Pipelines.CreateReportAnnotations;
using Bbx.Features.Pipelines.DeleteDeploymentEnvironment;
using Bbx.Features.Pipelines.DeleteDeploymentVariable;
using Bbx.Features.Pipelines.DeletePipelineKnownHost;
using Bbx.Features.Pipelines.DeletePipelineReport;
using Bbx.Features.Pipelines.DeletePipelineSshKeyPair;
using Bbx.Features.Pipelines.DeleteReportAnnotation;
using Bbx.Features.Pipelines.ListDeployments;
using Bbx.Features.Pipelines.ListDeploymentVariables;
using Bbx.Features.Pipelines.ListPipelineKnownHosts;
using Bbx.Features.Pipelines.ListPipelineScheduleExecutions;
using Bbx.Features.Pipelines.ListTestCaseReasons;
using Bbx.Features.Pipelines.PipelineCacheContentUri;
using Bbx.Features.Pipelines.SetPipelineBuildNumber;
using Bbx.Features.Pipelines.SetPipelineSshKeyPair;
using Bbx.Features.Pipelines.UpdateDeploymentVariable;
using Bbx.Features.Pipelines.UpdatePipelineKnownHost;
using Bbx.Features.Pipelines.UpdatePipelineSchedule;
using Bbx.Features.Pipelines.UpdatePipelinesConfig;
using Bbx.Features.Pipelines.UpdatePipelineVariable;
using Bbx.Features.Pipelines.UpsertPipelineReport;
using Bbx.Features.Pipelines.UpsertReportAnnotation;
using Bbx.Features.Pipelines.ViewDeployment;
using Bbx.Features.Pipelines.ViewPipelineKnownHost;
using Bbx.Features.Pipelines.ViewPipelineSchedule;
using Bbx.Features.Pipelines.ViewPipelinesConfig;
using Bbx.Features.Pipelines.ViewPipelineSshKeyPair;
using Bbx.Features.Pipelines.ViewPipelineStep;
using Bbx.Features.Pipelines.ViewPipelineVariable;
using Bbx.Features.Pipelines.ViewReportAnnotation;
using Bbx.Features.Pipelines.AddPipelineVariable;
using Bbx.Features.Pipelines.ClearPipelineCache;
using Bbx.Features.Pipelines.CreatePipelineSchedule;
using Bbx.Features.Pipelines.DeletePipelineSchedule;
using Bbx.Features.Pipelines.DeletePipelineVariable;
using Bbx.Features.Pipelines.ListDeploymentEnvironments;
using Bbx.Features.Pipelines.ListPipelineCaches;
using Bbx.Features.Pipelines.ListPipelineReports;
using Bbx.Features.Pipelines.ListPipelines;
using Bbx.Features.Pipelines.ListPipelineSchedules;
using Bbx.Features.Pipelines.ListPipelineSteps;
using Bbx.Features.Pipelines.ListPipelineVariables;
using Bbx.Features.Pipelines.ListReportAnnotations;
using Bbx.Features.Pipelines.ListTestCases;
using Bbx.Features.Pipelines.ListTestReports;
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
        command.Subcommands.Add(CreateConfigCommand(services));
        command.Subcommands.Add(CreateSshCommand(services));
        command.Subcommands.Add(CreateStepCommand(services));
        command.Subcommands.Add(CreateTestCaseReasonsCommand(services));
        command.Subcommands.Add(CreateDeploysCommand(services));
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
                    .HandleAsync(new ListPipelineReportsRequest(workspace, repo, hash, limit), CommandBinding.CancellationToken)),
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
                    .HandleAsync(new ViewPipelineReportRequest(workspace, repo, hash, reportId), CommandBinding.CancellationToken)),
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
                    .HandleAsync(new ListReportAnnotationsRequest(workspace, repo, hash, reportId, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, annHashArg, annReportIdArg, annLimitOption);
        reportsCommand.Subcommands.Add(annotationsCommand);

        var updateCommand = new Command("update",
            "Create or replace a report. The ID is yours to choose, so this works from a CI step that has no record of whether it ran before.");
        var upHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var upReportIdArg = new Argument<string>("report-id") { Description = "Report ID you choose" };
        var upTitleOption = new Option<string>("--title") { Description = "Report title", Required = true };
        var upDetailsOption = new Option<string>("--details")
        { Description = "Longer description. Required: Bitbucket refuses a report without it.", Required = true };
        var upTypeOption = new Option<string?>("--type") { Description = "Report type (SECURITY, COVERAGE, TEST, BUG). Defaults to TEST." };
        var upResultOption = new Option<string?>("--result") { Description = "Result (PASSED, FAILED, PENDING)" };
        var upLinkOption = new Option<string?>("--link") { Description = "URL to the full report" };
        updateCommand.Arguments.Add(upHashArg);
        updateCommand.Arguments.Add(upReportIdArg);
        updateCommand.Options.Add(upTitleOption);
        updateCommand.Options.Add(upDetailsOption);
        updateCommand.Options.Add(upTypeOption);
        updateCommand.Options.Add(upResultOption);
        updateCommand.Options.Add(upLinkOption);
        updateCommand.SetHandler((string? workspace, string? repo, string hash, string reportId, string title, string details, string? type, string? result, string? link) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpsertPipelineReportHandler>()
                    .HandleAsync(new UpsertPipelineReportRequest(workspace, repo, hash, reportId, title, details, type, result, link), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, upHashArg, upReportIdArg, upTitleOption, upDetailsOption, upTypeOption, upResultOption, upLinkOption);
        reportsCommand.Subcommands.Add(updateCommand);

        var deleteCommand = new Command("delete", "Delete a report and its annotations");
        var delHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var delReportIdArg = new Argument<string>("report-id") { Description = "Report ID" };
        var delYesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        deleteCommand.Arguments.Add(delHashArg);
        deleteCommand.Arguments.Add(delReportIdArg);
        deleteCommand.Options.Add(delYesOption);
        deleteCommand.SetHandler(async (string? workspace, string? repo, string hash, string reportId, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr(
                    $"Delete report {reportId} and its annotations? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeletePipelineReportHandler>()
                    .HandleAsync(new DeletePipelineReportRequest(workspace, repo, hash, reportId), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, delHashArg, delReportIdArg, delYesOption);
        reportsCommand.Subcommands.Add(deleteCommand);

        var annotationsCreateCommand = new Command("annotations-create",
            "Add annotations to a report in one call");
        var acHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var acReportIdArg = new Argument<string>("report-id") { Description = "Report ID" };
        var acJsonOption = new Option<string>("--annotations")
        {
            Description = "JSON array of annotation objects, e.g. "
                + "'[{\"external_id\":\"a1\",\"summary\":\"Unused import\",\"annotation_type\":\"CODE_SMELL\",\"path\":\"src/a.cs\",\"line\":3}]'",
            Required = true,
        };
        annotationsCreateCommand.Arguments.Add(acHashArg);
        annotationsCreateCommand.Arguments.Add(acReportIdArg);
        annotationsCreateCommand.Options.Add(acJsonOption);
        annotationsCreateCommand.SetHandler((string? workspace, string? repo, string hash, string reportId, string json) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateReportAnnotationsHandler>()
                    .HandleAsync(new CreateReportAnnotationsRequest(workspace, repo, hash, reportId, json), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, acHashArg, acReportIdArg, acJsonOption);
        reportsCommand.Subcommands.Add(annotationsCreateCommand);

        var annotationViewCommand = new Command("annotation-view", "View one annotation");
        var avHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var avReportIdArg = new Argument<string>("report-id") { Description = "Report ID" };
        var avAnnotationIdArg = new Argument<string>("annotation-id") { Description = "Annotation ID" };
        annotationViewCommand.Arguments.Add(avHashArg);
        annotationViewCommand.Arguments.Add(avReportIdArg);
        annotationViewCommand.Arguments.Add(avAnnotationIdArg);
        annotationViewCommand.SetHandler((string? workspace, string? repo, string hash, string reportId, string annotationId) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewReportAnnotationHandler>()
                    .HandleAsync(new ViewReportAnnotationRequest(workspace, repo, hash, reportId, annotationId), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, avHashArg, avReportIdArg, avAnnotationIdArg);
        reportsCommand.Subcommands.Add(annotationViewCommand);

        var annotationUpdateCommand = new Command("annotation-update", "Create or replace one annotation");
        var auHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var auReportIdArg = new Argument<string>("report-id") { Description = "Report ID" };
        var auAnnotationIdArg = new Argument<string>("annotation-id") { Description = "Annotation ID you choose" };
        var auSummaryOption = new Option<string>("--summary") { Description = "One-line summary", Required = true };
        var auDetailsOption = new Option<string?>("--details") { Description = "Longer description" };
        var auTypeOption = new Option<string?>("--type") { Description = "Annotation type (VULNERABILITY, CODE_SMELL, BUG)" };
        var auSeverityOption = new Option<string?>("--severity") { Description = "Severity (CRITICAL, HIGH, MEDIUM, LOW)" };
        var auPathOption = new Option<string?>("--path") { Description = "File the annotation points at" };
        var auLineOption = new Option<int?>("--line") { Description = "Line of --path the annotation points at" };
        annotationUpdateCommand.Arguments.Add(auHashArg);
        annotationUpdateCommand.Arguments.Add(auReportIdArg);
        annotationUpdateCommand.Arguments.Add(auAnnotationIdArg);
        foreach (var option in new Option[] { auSummaryOption, auDetailsOption, auTypeOption, auSeverityOption, auPathOption, auLineOption })
        {
            annotationUpdateCommand.Options.Add(option);
        }
        // Nine bound symbols is the ceiling, and this needs eleven, so it reads
        // the parse result itself the way repo update does.
        annotationUpdateCommand.SetAction((parseResult, _) => CommandRunner.RunJsonAsync(() =>
            services.GetRequiredService<UpsertReportAnnotationHandler>().HandleAsync(
                new UpsertReportAnnotationRequest(
                    parseResult.GetValue(workspaceOption),
                    parseResult.GetValue(repoOption),
                    parseResult.GetValue(auHashArg)!,
                    parseResult.GetValue(auReportIdArg)!,
                    parseResult.GetValue(auAnnotationIdArg)!,
                    parseResult.GetValue(auSummaryOption)!,
                    parseResult.GetValue(auDetailsOption),
                    parseResult.GetValue(auTypeOption),
                    parseResult.GetValue(auSeverityOption),
                    parseResult.GetValue(auPathOption),
                    parseResult.GetValue(auLineOption)),
                CommandBinding.CancellationToken)));
        reportsCommand.Subcommands.Add(annotationUpdateCommand);

        var annotationDeleteCommand = new Command("annotation-delete", "Delete one annotation");
        var adHashArg = new Argument<string>("hash") { Description = "Commit hash" };
        var adReportIdArg = new Argument<string>("report-id") { Description = "Report ID" };
        var adAnnotationIdArg = new Argument<string>("annotation-id") { Description = "Annotation ID" };
        var adYesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        annotationDeleteCommand.Arguments.Add(adHashArg);
        annotationDeleteCommand.Arguments.Add(adReportIdArg);
        annotationDeleteCommand.Arguments.Add(adAnnotationIdArg);
        annotationDeleteCommand.Options.Add(adYesOption);
        annotationDeleteCommand.SetHandler(async (string? workspace, string? repo, string hash, string reportId, string annotationId, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete annotation {annotationId}? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteReportAnnotationHandler>()
                    .HandleAsync(new DeleteReportAnnotationRequest(workspace, repo, hash, reportId, annotationId), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, adHashArg, adReportIdArg, adAnnotationIdArg, adYesOption);
        reportsCommand.Subcommands.Add(annotationDeleteCommand);

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
                    .HandleAsync(new ListTestReportsRequest(workspace, repo, pipelineUuid, stepUuid), CommandBinding.CancellationToken)),
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
                    .HandleAsync(new ListTestCasesRequest(workspace, repo, pipelineUuid, stepUuid, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, pipelineUuidArg, stepUuidArg, limitOption);
        return command;
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
                    .HandleAsync(new ListPipelinesRequest(workspace, repo, status, sort, limit, branch), CommandBinding.CancellationToken)),
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
                    .HandleAsync(new ViewPipelineRequest(workspace, repo, pipelineUuid), CommandBinding.CancellationToken)),
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
        var variablesOption = new Option<string[]>("--variable") { Description = "Pipeline variables in key=value format", AllowMultipleArgumentsPerToken = true };

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
                    .HandleAsync(new TriggerPipelineRequest(workspace, repo, branch, commit, pattern, pullRequest, variables), CommandBinding.CancellationToken)),
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
                    .HandleAsync(new StopPipelineRequest(workspace, repo, pipelineUuid), CommandBinding.CancellationToken));
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
        var logUuidOption = new Option<string?>("--log-uuid")
        { Description = "Read one numbered log rather than the whole step. A step that reran has one per attempt." };

        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Arguments.Add(pipelineArg);
        command.Arguments.Add(stepArg);
        command.Options.Add(followOption);
        command.Options.Add(logUuidOption);

        command.SetHandler((string? workspace, string? repo, string pipelineUuid, string stepUuid, bool follow, string? logUuid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<PipelineLogsHandler>()
                    .HandleAsync(new PipelineLogsRequest(workspace, repo, pipelineUuid, stepUuid, logUuid), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, pipelineArg, stepArg, followOption, logUuidOption);
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
                    .HandleAsync(new ListPipelineStepsRequest(workspace, repo, pipelineUuid), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, pipelineArg);
        return command;
    }

    private static Command CreateVariablesCommand(IServiceProvider services)
    {
        var command = new Command("variables", "Manage pipeline variables");
        command.Subcommands.Add(CreateVariablesListCommand(services));
        command.Subcommands.Add(CreateVariablesAddCommand(services));
        command.Subcommands.Add(CreateVariablesDeleteCommand(services));
        command.Subcommands.Add(CreateVariablesViewCommand(services));
        command.Subcommands.Add(CreateVariablesUpdateCommand(services));
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
                    .HandleAsync(new ListPipelineVariablesRequest(workspace, repo), CommandBinding.CancellationToken)),
            workspaceOption, repoOption);
        return command;
    }

    private static Command CreateVariablesAddCommand(IServiceProvider services)
    {
        var command = new Command("add", "Add a pipeline variable");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var keyOption = new Option<string>("--key") { Description = "Variable key", Required = true };
        var valueOption = new Option<string>("--value") { Description = "Variable value", Required = true };
        var securedOption = new Option<bool>("--secured") { Description = "Mark as secured (value hidden)" };

        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Options.Add(keyOption);
        command.Options.Add(valueOption);
        command.Options.Add(securedOption);

        command.SetHandler((string? workspace, string? repo, string key, string value, bool secured) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddPipelineVariableHandler>()
                    .HandleAsync(new AddPipelineVariableRequest(workspace, repo, key, value, secured), CommandBinding.CancellationToken)),
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
                    .HandleAsync(new DeletePipelineVariableRequest(workspace, repo, uuid), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, uuidArg, yesOption);
        return command;
    }

    private static Command CreateSchedulesCommand(IServiceProvider services)
    {
        var command = new Command("schedules", "Manage pipeline schedules");
        command.Subcommands.Add(CreateSchedulesListCommand(services));
        command.Subcommands.Add(CreateSchedulesCreateCommand(services));
        command.Subcommands.Add(CreateSchedulesDeleteCommand(services));
        command.Subcommands.Add(CreateSchedulesViewCommand(services));
        command.Subcommands.Add(CreateSchedulesUpdateCommand(services));
        command.Subcommands.Add(CreateSchedulesExecutionsCommand(services));
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
                    .HandleAsync(new ListPipelineSchedulesRequest(workspace, repo), CommandBinding.CancellationToken)),
            workspaceOption, repoOption);
        return command;
    }

    private static Command CreateSchedulesCreateCommand(IServiceProvider services)
    {
        var command = new Command("create", "Create a pipeline schedule");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var cronOption = new Option<string>("--cron") { Description = "Cron expression (e.g., '0 0 * * *')", Required = true };
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
                    .HandleAsync(new CreatePipelineScheduleRequest(workspace, repo, cron, branch, pattern, enabled), CommandBinding.CancellationToken)),
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
                    .HandleAsync(new DeletePipelineScheduleRequest(workspace, repo, uuid), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, uuidArg, yesOption);
        return command;
    }

    private static Command CreateCachesCommand(IServiceProvider services)
    {
        var command = new Command("caches", "Manage pipeline caches");
        command.Subcommands.Add(CreateCachesListCommand(services));
        command.Subcommands.Add(CreateCachesClearCommand(services));
        command.Subcommands.Add(CreateCachesContentUriCommand(services));
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
                    .HandleAsync(new ListPipelineCachesRequest(workspace, repo), CommandBinding.CancellationToken)),
            workspaceOption, repoOption);
        return command;
    }

    private static Command CreateCachesClearCommand(IServiceProvider services)
    {
        var command = new Command("clear", "Clear one pipeline cache by UUID, or every cache with a given --name");
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        var nameArg = new Argument<string?>("cache-uuid")
        {
            Description = "Cache UUID to clear. Omit and pass --name to clear by name instead.",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => null,
        };
        var nameOption = new Option<string?>("--name")
        { Description = "Clear every cache with this name, whatever its UUID" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation prompt" };

        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        command.Arguments.Add(nameArg);
        command.Options.Add(nameOption);
        command.Options.Add(yesOption);

        command.SetHandler(async (string? workspace, string? repo, string? uuid, string? name, bool yes) =>
        {
            // Two endpoints: one clears a single cache by UUID, the other
            // clears every cache with a given name whatever its UUID.
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(uuid))
                throw new BbxUserException("Error: pass a cache UUID or --name, not both.");
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(uuid))
                throw new BbxUserException("Error: name a cache UUID to clear, or pass --name.");

            var prompt = string.IsNullOrEmpty(name)
                ? $"Clear cache {uuid}? [y/N]: "
                : $"Clear every cache named '{name}'? [y/N]: ";
            if (!yes && !CommandRunner.ConfirmOrCancelStderr(prompt))
                return;

            await CommandRunner.RunJsonAsync(() => string.IsNullOrEmpty(name)
                ? services.GetRequiredService<ClearPipelineCacheHandler>()
                    .HandleAsync(new ClearPipelineCacheRequest(workspace, repo, uuid!), CommandBinding.CancellationToken)
                : services.GetRequiredService<ClearAllPipelineCachesHandler>()
                    .HandleAsync(new ClearAllPipelineCachesRequest(workspace, repo, name), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, nameArg, nameOption, yesOption);
        return command;
    }

    private static Command CreateDeploymentsCommand(IServiceProvider services)
    {
        var command = new Command("deployments", "Manage deployment environments");
        command.Subcommands.Add(CreateDeploymentsListCommand(services));
        command.Subcommands.Add(CreateDeploymentsViewCommand(services));
        command.Subcommands.Add(CreateDeploymentsCreateCommand(services));
        command.Subcommands.Add(CreateDeploymentsDeleteCommand(services));
        command.Subcommands.Add(CreateDeploymentsChangesCommand(services));
        command.Subcommands.Add(CreateDeploymentVariablesCommand(services));
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
                    .HandleAsync(new ListDeploymentEnvironmentsRequest(workspace, repo), CommandBinding.CancellationToken)),
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
                    .HandleAsync(new ViewDeploymentEnvironmentRequest(workspace, repo, environment), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, envArg);
        return command;
    }

    /// <summary>
    /// The workspace and repository options every command in this group takes.
    /// </summary>
    private static (Option<string?> Workspace, Option<string?> Repo) AddScope(Command command)
    {
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        command.Options.Add(workspaceOption);
        command.Options.Add(repoOption);
        return (workspaceOption, repoOption);
    }

    private static (Option<string?> Workspace, Option<string?> Repo) AddRecursiveScope(Command command)
    {
        var workspaceOption = new Option<string?>("--workspace", "-w") { Description = "Workspace slug" };
        var repoOption = new Option<string?>("--repo", "-r") { Description = "Repository slug" };
        command.AddRecursiveOption(workspaceOption);
        command.AddRecursiveOption(repoOption);
        return (workspaceOption, repoOption);
    }

    private static Command CreateConfigCommand(IServiceProvider services)
    {
        var configCommand = new Command("config", "Turn Pipelines on or off and set the next build number");
        var (workspaceOption, repoOption) = AddRecursiveScope(configCommand);

        var viewCommand = new Command("view", "Show whether Pipelines is enabled");
        viewCommand.SetHandler((string? workspace, string? repo) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewPipelinesConfigHandler>()
                    .HandleAsync(new ViewPipelinesConfigRequest(workspace, repo), CommandBinding.CancellationToken)),
            workspaceOption, repoOption);
        configCommand.Subcommands.Add(viewCommand);

        var updateCommand = new Command("update", "Enable or disable Pipelines for the repository");
        var enabledOption = new Option<bool>("--enabled") { Description = "Enable Pipelines" };
        var disabledOption = new Option<bool>("--disabled") { Description = "Disable Pipelines" };
        updateCommand.Options.Add(enabledOption);
        updateCommand.Options.Add(disabledOption);
        updateCommand.SetHandler((string? workspace, string? repo, bool enabled, bool disabled) =>
            CommandRunner.RunJsonAsync(() =>
            {
                if (enabled == disabled)
                    throw new BbxUserException("Error: pass exactly one of --enabled or --disabled.");
                return services.GetRequiredService<UpdatePipelinesConfigHandler>()
                    .HandleAsync(new UpdatePipelinesConfigRequest(workspace, repo, enabled), CommandBinding.CancellationToken);
            }),
            workspaceOption, repoOption, enabledOption, disabledOption);
        configCommand.Subcommands.Add(updateCommand);

        var buildNumberCommand = new Command("build-number", "Set the number the next pipeline run will use");
        var nextOption = new Option<int>("--next") { Description = "Next build number", Required = true };
        buildNumberCommand.Options.Add(nextOption);
        buildNumberCommand.SetHandler((string? workspace, string? repo, int next) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<SetPipelineBuildNumberHandler>()
                    .HandleAsync(new SetPipelineBuildNumberRequest(workspace, repo, next), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, nextOption);
        configCommand.Subcommands.Add(buildNumberCommand);

        return configCommand;
    }

    private static Command CreateSshCommand(IServiceProvider services)
    {
        var sshCommand = new Command("ssh", "The SSH key pair and known hosts a pipeline uses to reach other servers");
        var (workspaceOption, repoOption) = AddRecursiveScope(sshCommand);

        var keyPairCommand = new Command("key-pair", "The SSH key pair pipelines authenticate with");

        var keyPairViewCommand = new Command("view", "Show the public half of the key pair");
        keyPairViewCommand.SetHandler((string? workspace, string? repo) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewPipelineSshKeyPairHandler>()
                    .HandleAsync(new ViewPipelineSshKeyPairRequest(workspace, repo), CommandBinding.CancellationToken)),
            workspaceOption, repoOption);
        keyPairCommand.Subcommands.Add(keyPairViewCommand);

        var keyPairSetCommand = new Command("set", "Replace the key pair");
        var privateKeyOption = new Option<string>("--private-key")
        { Description = "Private key body. Bitbucket stores it and never returns it.", Required = true };
        var publicKeyOption = new Option<string>("--public-key") { Description = "Matching public key body", Required = true };
        keyPairSetCommand.Options.Add(privateKeyOption);
        keyPairSetCommand.Options.Add(publicKeyOption);
        keyPairSetCommand.SetHandler((string? workspace, string? repo, string privateKey, string publicKey) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<SetPipelineSshKeyPairHandler>()
                    .HandleAsync(new SetPipelineSshKeyPairRequest(workspace, repo, privateKey, publicKey), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, privateKeyOption, publicKeyOption);
        keyPairCommand.Subcommands.Add(keyPairSetCommand);

        var keyPairDeleteCommand = new Command("delete", "Delete the key pair");
        var keyPairYesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        keyPairDeleteCommand.Options.Add(keyPairYesOption);
        keyPairDeleteCommand.SetHandler(async (string? workspace, string? repo, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr(
                    "Delete the pipelines SSH key pair? Any pipeline using it will stop authenticating. [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeletePipelineSshKeyPairHandler>()
                    .HandleAsync(new DeletePipelineSshKeyPairRequest(workspace, repo), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, keyPairYesOption);
        keyPairCommand.Subcommands.Add(keyPairDeleteCommand);

        sshCommand.Subcommands.Add(keyPairCommand);

        var knownHostsCommand = new Command("known-hosts", "The host keys a pipeline will trust");

        var khListCommand = new Command("list", "List known hosts");
        var khLimitOption = new Option<int>("--limit") { Description = "Maximum hosts to list", DefaultValueFactory = _ => 50 };
        khListCommand.Options.Add(khLimitOption);
        khListCommand.SetHandler((string? workspace, string? repo, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListPipelineKnownHostsHandler>()
                    .HandleAsync(new ListPipelineKnownHostsRequest(workspace, repo, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, khLimitOption);
        knownHostsCommand.Subcommands.Add(khListCommand);

        var khAddCommand = new Command("add", "Trust a host key");
        var khHostnameOption = new Option<string>("--hostname") { Description = "Hostname, e.g. bitbucket.org", Required = true };
        var khKeyTypeOption = new Option<string>("--key-type") { Description = "Key type, e.g. ssh-rsa or ssh-ed25519", Required = true };
        var khKeyOption = new Option<string>("--key") { Description = "Base64 host key body", Required = true };
        khAddCommand.Options.Add(khHostnameOption);
        khAddCommand.Options.Add(khKeyTypeOption);
        khAddCommand.Options.Add(khKeyOption);
        khAddCommand.SetHandler((string? workspace, string? repo, string hostname, string keyType, string key) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddPipelineKnownHostHandler>()
                    .HandleAsync(new AddPipelineKnownHostRequest(workspace, repo, hostname, keyType, key), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, khHostnameOption, khKeyTypeOption, khKeyOption);
        knownHostsCommand.Subcommands.Add(khAddCommand);

        var khViewCommand = new Command("view", "View a known host");
        var khViewUuidArg = new Argument<string>("host-uuid") { Description = "Known host UUID" };
        khViewCommand.Arguments.Add(khViewUuidArg);
        khViewCommand.SetHandler((string? workspace, string? repo, string hostUuid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewPipelineKnownHostHandler>()
                    .HandleAsync(new ViewPipelineKnownHostRequest(workspace, repo, hostUuid), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, khViewUuidArg);
        knownHostsCommand.Subcommands.Add(khViewCommand);

        var khUpdateCommand = new Command("update", "Replace a known host");
        var khUpdateUuidArg = new Argument<string>("host-uuid") { Description = "Known host UUID" };
        var khUpdateHostnameOption = new Option<string>("--hostname") { Description = "Hostname", Required = true };
        var khUpdateKeyTypeOption = new Option<string>("--key-type")
        { Description = "Key type. Required: this PUT replaces rather than merges.", Required = true };
        var khUpdateKeyOption = new Option<string>("--key") { Description = "Base64 host key body", Required = true };
        khUpdateCommand.Arguments.Add(khUpdateUuidArg);
        khUpdateCommand.Options.Add(khUpdateHostnameOption);
        khUpdateCommand.Options.Add(khUpdateKeyTypeOption);
        khUpdateCommand.Options.Add(khUpdateKeyOption);
        khUpdateCommand.SetHandler((string? workspace, string? repo, string hostUuid, string hostname, string keyType, string key) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<UpdatePipelineKnownHostHandler>()
                    .HandleAsync(new UpdatePipelineKnownHostRequest(workspace, repo, hostUuid, hostname, keyType, key), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, khUpdateUuidArg, khUpdateHostnameOption, khUpdateKeyTypeOption, khUpdateKeyOption);
        knownHostsCommand.Subcommands.Add(khUpdateCommand);

        var khDeleteCommand = new Command("delete", "Stop trusting a host key");
        var khDeleteUuidArg = new Argument<string>("host-uuid") { Description = "Known host UUID" };
        var khYesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        khDeleteCommand.Arguments.Add(khDeleteUuidArg);
        khDeleteCommand.Options.Add(khYesOption);
        khDeleteCommand.SetHandler(async (string? workspace, string? repo, string hostUuid, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete known host {hostUuid}? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeletePipelineKnownHostHandler>()
                    .HandleAsync(new DeletePipelineKnownHostRequest(workspace, repo, hostUuid), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, khDeleteUuidArg, khYesOption);
        knownHostsCommand.Subcommands.Add(khDeleteCommand);

        sshCommand.Subcommands.Add(knownHostsCommand);

        return sshCommand;
    }

    private static Command CreateStepCommand(IServiceProvider services)
    {
        var command = new Command("step", "View one step of a pipeline");
        var (workspaceOption, repoOption) = AddScope(command);
        var pipelineArg = new Argument<string>("pipeline-uuid") { Description = "Pipeline UUID" };
        var stepArg = new Argument<string>("step-uuid") { Description = "Step UUID" };
        command.Arguments.Add(pipelineArg);
        command.Arguments.Add(stepArg);
        command.SetHandler((string? workspace, string? repo, string pipelineUuid, string stepUuid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewPipelineStepHandler>()
                    .HandleAsync(new ViewPipelineStepRequest(workspace, repo, pipelineUuid, stepUuid), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, pipelineArg, stepArg);
        return command;
    }

    private static Command CreateTestCaseReasonsCommand(IServiceProvider services)
    {
        var command = new Command("test-case-reasons",
            "Why a test case failed: the stack trace and the assertion message");
        var (workspaceOption, repoOption) = AddScope(command);
        var pipelineArg = new Argument<string>("pipeline-uuid") { Description = "Pipeline UUID" };
        var stepArg = new Argument<string>("step-uuid") { Description = "Step UUID" };
        var testCaseArg = new Argument<string>("test-case-uuid") { Description = "Test case UUID" };
        var limitOption = new Option<int>("--limit") { Description = "Maximum reasons to list", DefaultValueFactory = _ => 50 };
        command.Arguments.Add(pipelineArg);
        command.Arguments.Add(stepArg);
        command.Arguments.Add(testCaseArg);
        command.Options.Add(limitOption);
        command.SetHandler((string? workspace, string? repo, string pipelineUuid, string stepUuid, string testCaseUuid, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListTestCaseReasonsHandler>()
                    .HandleAsync(new ListTestCaseReasonsRequest(workspace, repo, pipelineUuid, stepUuid, testCaseUuid, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, pipelineArg, stepArg, testCaseArg, limitOption);
        return command;
    }

    /// <summary>
    /// Deployments, meaning the records of what was released where. The
    /// environments they target are managed under `pipeline deployments`.
    /// </summary>
    private static Command CreateDeploysCommand(IServiceProvider services)
    {
        var deploysCommand = new Command("deploys",
            "Deployment records: what was released to which environment");
        var (workspaceOption, repoOption) = AddRecursiveScope(deploysCommand);

        var listCommand = new Command("list", "List deployments");
        var limitOption = new Option<int>("--limit") { Description = "Maximum deployments to list", DefaultValueFactory = _ => 25 };
        listCommand.Options.Add(limitOption);
        listCommand.SetHandler((string? workspace, string? repo, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListDeploymentsHandler>()
                    .HandleAsync(new ListDeploymentsRequest(workspace, repo, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, limitOption);
        deploysCommand.Subcommands.Add(listCommand);

        var viewCommand = new Command("view", "View one deployment");
        var uuidArg = new Argument<string>("deployment-uuid") { Description = "Deployment UUID" };
        viewCommand.Arguments.Add(uuidArg);
        viewCommand.SetHandler((string? workspace, string? repo, string deploymentUuid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewDeploymentHandler>()
                    .HandleAsync(new ViewDeploymentRequest(workspace, repo, deploymentUuid), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, uuidArg);
        deploysCommand.Subcommands.Add(viewCommand);

        return deploysCommand;
    }


    private static Command CreateSchedulesViewCommand(IServiceProvider services)
    {
        var command = new Command("view", "View a pipeline schedule");
        var (workspaceOption, repoOption) = AddScope(command);
        var uuidArg = new Argument<string>("schedule-uuid") { Description = "Schedule UUID" };
        command.Arguments.Add(uuidArg);
        command.SetHandler((string? workspace, string? repo, string scheduleUuid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewPipelineScheduleHandler>()
                    .HandleAsync(new ViewPipelineScheduleRequest(workspace, repo, scheduleUuid), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, uuidArg);
        return command;
    }

    private static Command CreateSchedulesUpdateCommand(IServiceProvider services)
    {
        var command = new Command("update", "Change a schedule's cron pattern or turn it on and off");
        var (workspaceOption, repoOption) = AddScope(command);
        var uuidArg = new Argument<string>("schedule-uuid") { Description = "Schedule UUID" };
        var cronOption = new Option<string?>("--cron") { Description = "New cron pattern" };
        var enabledOption = new Option<bool>("--enabled") { Description = "Enable the schedule" };
        var disabledOption = new Option<bool>("--disabled") { Description = "Disable the schedule" };
        command.Arguments.Add(uuidArg);
        command.Options.Add(cronOption);
        command.Options.Add(enabledOption);
        command.Options.Add(disabledOption);
        command.SetHandler((string? workspace, string? repo, string scheduleUuid, string? cron, bool enabled, bool disabled) =>
            CommandRunner.RunJsonAsync(() =>
            {
                if (enabled && disabled)
                    throw new BbxUserException("Error: --enabled and --disabled are mutually exclusive.");
                bool? on = enabled ? true : disabled ? false : null;
                return services.GetRequiredService<UpdatePipelineScheduleHandler>()
                    .HandleAsync(new UpdatePipelineScheduleRequest(workspace, repo, scheduleUuid, on, cron), CommandBinding.CancellationToken);
            }),
            workspaceOption, repoOption, uuidArg, cronOption, enabledOption, disabledOption);
        return command;
    }

    private static Command CreateSchedulesExecutionsCommand(IServiceProvider services)
    {
        var command = new Command("executions", "List the runs a schedule has produced");
        var (workspaceOption, repoOption) = AddScope(command);
        var uuidArg = new Argument<string>("schedule-uuid") { Description = "Schedule UUID" };
        var limitOption = new Option<int>("--limit") { Description = "Maximum executions to list", DefaultValueFactory = _ => 25 };
        command.Arguments.Add(uuidArg);
        command.Options.Add(limitOption);
        command.SetHandler((string? workspace, string? repo, string scheduleUuid, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListPipelineScheduleExecutionsHandler>()
                    .HandleAsync(new ListPipelineScheduleExecutionsRequest(workspace, repo, scheduleUuid, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, uuidArg, limitOption);
        return command;
    }

    private static Command CreateVariablesViewCommand(IServiceProvider services)
    {
        var command = new Command("view", "View one pipeline variable");
        var (workspaceOption, repoOption) = AddScope(command);
        var uuidArg = new Argument<string>("variable-uuid") { Description = "Variable UUID" };
        command.Arguments.Add(uuidArg);
        command.SetHandler((string? workspace, string? repo, string variableUuid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ViewPipelineVariableHandler>()
                    .HandleAsync(new ViewPipelineVariableRequest(workspace, repo, variableUuid), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, uuidArg);
        return command;
    }

    private static Command CreateVariablesUpdateCommand(IServiceProvider services)
    {
        var command = new Command("update", "Change a pipeline variable");
        var (workspaceOption, repoOption) = AddScope(command);
        var uuidArg = new Argument<string>("variable-uuid") { Description = "Variable UUID" };
        var keyOption = new Option<string?>("--key") { Description = "New variable name" };
        var valueOption = new Option<string?>("--value") { Description = "New value" };
        var securedOption = new Option<bool>("--secured") { Description = "Hide the value from the API and the UI" };
        var unsecuredOption = new Option<bool>("--unsecured") { Description = "Show the value again" };
        command.Arguments.Add(uuidArg);
        command.Options.Add(keyOption);
        command.Options.Add(valueOption);
        command.Options.Add(securedOption);
        command.Options.Add(unsecuredOption);
        command.SetHandler((string? workspace, string? repo, string variableUuid, string? key, string? value, bool secured, bool unsecured) =>
            CommandRunner.RunJsonAsync(() =>
            {
                if (secured && unsecured)
                    throw new BbxUserException("Error: --secured and --unsecured are mutually exclusive.");
                bool? isSecured = secured ? true : unsecured ? false : null;
                return services.GetRequiredService<UpdatePipelineVariableHandler>()
                    .HandleAsync(new UpdatePipelineVariableRequest(workspace, repo, variableUuid, key, value, isSecured), CommandBinding.CancellationToken);
            }),
            workspaceOption, repoOption, uuidArg, keyOption, valueOption, securedOption, unsecuredOption);
        return command;
    }

    private static Command CreateCachesContentUriCommand(IServiceProvider services)
    {
        var command = new Command("content-uri", "Get a signed URL for a cache archive");
        var (workspaceOption, repoOption) = AddScope(command);
        var uuidArg = new Argument<string>("cache-uuid") { Description = "Cache UUID, as shown by caches list" };
        command.Arguments.Add(uuidArg);
        command.SetHandler((string? workspace, string? repo, string cacheUuid) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<PipelineCacheContentUriHandler>()
                    .HandleAsync(new PipelineCacheContentUriRequest(workspace, repo, cacheUuid), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, uuidArg);
        return command;
    }

    private static Command CreateDeploymentsCreateCommand(IServiceProvider services)
    {
        var command = new Command("create", "Create a deployment environment");
        var (workspaceOption, repoOption) = AddScope(command);
        var nameOption = new Option<string>("--name") { Description = "Environment name", Required = true };
        var typeOption = new Option<string>("--type")
        { Description = "Environment type (Test, Staging, Production)", DefaultValueFactory = _ => "Test" };
        var rankOption = new Option<int>("--rank")
        { Description = "Position in the deployment view. Bitbucket refuses a body without it.", DefaultValueFactory = _ => 0 };
        command.Options.Add(nameOption);
        command.Options.Add(typeOption);
        command.Options.Add(rankOption);
        command.SetHandler((string? workspace, string? repo, string name, string type, int rank) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<CreateDeploymentEnvironmentHandler>()
                    .HandleAsync(new CreateDeploymentEnvironmentRequest(workspace, repo, name, type, rank), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, nameOption, typeOption, rankOption);
        return command;
    }

    private static Command CreateDeploymentsDeleteCommand(IServiceProvider services)
    {
        var command = new Command("delete", "Delete a deployment environment");
        var (workspaceOption, repoOption) = AddScope(command);
        var envArg = new Argument<string>("environment") { Description = "Environment UUID" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        command.Arguments.Add(envArg);
        command.Options.Add(yesOption);
        command.SetHandler(async (string? workspace, string? repo, string environment, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr(
                    $"Delete environment {environment} and its variables? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteDeploymentEnvironmentHandler>()
                    .HandleAsync(new DeleteDeploymentEnvironmentRequest(workspace, repo, environment), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, envArg, yesOption);
        return command;
    }

    private static Command CreateDeploymentsChangesCommand(IServiceProvider services)
    {
        var command = new Command("changes", "Rename a deployment environment, or restrict who can deploy to it");
        var (workspaceOption, repoOption) = AddScope(command);
        var envArg = new Argument<string>("environment") { Description = "Environment UUID" };
        var nameOption = new Option<string?>("--name") { Description = "New environment name" };
        var adminOnlyOption = new Option<bool>("--admin-only") { Description = "Let only admins deploy to the environment" };
        var noAdminOnlyOption = new Option<bool>("--no-admin-only") { Description = "Let anyone with access deploy to the environment" };
        command.Arguments.Add(envArg);
        command.Options.Add(nameOption);
        command.Options.Add(adminOnlyOption);
        command.Options.Add(noAdminOnlyOption);
        command.SetHandler((string? workspace, string? repo, string environment, string? name, bool adminOnly, bool noAdminOnly) =>
            CommandRunner.RunActionAsync(() =>
            {
                if (adminOnly && noAdminOnly)
                    throw new BbxUserException("Error: --admin-only and --no-admin-only are mutually exclusive.");
                bool? restricted = adminOnly ? true : noAdminOnly ? false : null;
                return services.GetRequiredService<ChangeDeploymentEnvironmentHandler>()
                    .HandleAsync(new ChangeDeploymentEnvironmentRequest(workspace, repo, environment, name, restricted), CommandBinding.CancellationToken);
            }),
            workspaceOption, repoOption, envArg, nameOption, adminOnlyOption, noAdminOnlyOption);
        return command;
    }

    private static Command CreateDeploymentVariablesCommand(IServiceProvider services)
    {
        var variablesCommand = new Command("variables", "Variables scoped to one deployment environment");
        var (workspaceOption, repoOption) = AddRecursiveScope(variablesCommand);
        var envOption = new Option<string>("--environment", "-e")
        { Description = "Environment UUID", Required = true };
        variablesCommand.AddRecursiveOption(envOption);

        var listCommand = new Command("list", "List deployment variables");
        var limitOption = new Option<int>("--limit") { Description = "Maximum variables to list", DefaultValueFactory = _ => 50 };
        listCommand.Options.Add(limitOption);
        listCommand.SetHandler((string? workspace, string? repo, string environment, int limit) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<ListDeploymentVariablesHandler>()
                    .HandleAsync(new ListDeploymentVariablesRequest(workspace, repo, environment, limit), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, envOption, limitOption);
        variablesCommand.Subcommands.Add(listCommand);

        var addCommand = new Command("add", "Add a deployment variable");
        var addKeyOption = new Option<string>("--key") { Description = "Variable name", Required = true };
        var addValueOption = new Option<string>("--value") { Description = "Variable value", Required = true };
        var addSecuredOption = new Option<bool>("--secured") { Description = "Hide the value from the API and the UI" };
        addCommand.Options.Add(addKeyOption);
        addCommand.Options.Add(addValueOption);
        addCommand.Options.Add(addSecuredOption);
        addCommand.SetHandler((string? workspace, string? repo, string environment, string key, string value, bool secured) =>
            CommandRunner.RunJsonAsync(() =>
                services.GetRequiredService<AddDeploymentVariableHandler>()
                    .HandleAsync(new AddDeploymentVariableRequest(workspace, repo, environment, key, value, secured), CommandBinding.CancellationToken)),
            workspaceOption, repoOption, envOption, addKeyOption, addValueOption, addSecuredOption);
        variablesCommand.Subcommands.Add(addCommand);

        var updateCommand = new Command("update", "Change a deployment variable");
        var updateUuidArg = new Argument<string>("variable-uuid") { Description = "Variable UUID" };
        var updateKeyOption = new Option<string?>("--key") { Description = "New variable name" };
        var updateValueOption = new Option<string?>("--value") { Description = "New value" };
        var updateSecuredOption = new Option<bool>("--secured") { Description = "Hide the value" };
        var updateUnsecuredOption = new Option<bool>("--unsecured") { Description = "Show the value again" };
        updateCommand.Arguments.Add(updateUuidArg);
        updateCommand.Options.Add(updateKeyOption);
        updateCommand.Options.Add(updateValueOption);
        updateCommand.Options.Add(updateSecuredOption);
        updateCommand.Options.Add(updateUnsecuredOption);
        updateCommand.SetHandler((string? workspace, string? repo, string environment, string variableUuid, string? key, string? value, bool secured, bool unsecured) =>
            CommandRunner.RunJsonAsync(() =>
            {
                if (secured && unsecured)
                    throw new BbxUserException("Error: --secured and --unsecured are mutually exclusive.");
                bool? isSecured = secured ? true : unsecured ? false : null;
                return services.GetRequiredService<UpdateDeploymentVariableHandler>()
                    .HandleAsync(new UpdateDeploymentVariableRequest(workspace, repo, environment, variableUuid, key, value, isSecured), CommandBinding.CancellationToken);
            }),
            workspaceOption, repoOption, envOption, updateUuidArg, updateKeyOption, updateValueOption, updateSecuredOption, updateUnsecuredOption);
        variablesCommand.Subcommands.Add(updateCommand);

        var deleteCommand = new Command("delete", "Delete a deployment variable");
        var deleteUuidArg = new Argument<string>("variable-uuid") { Description = "Variable UUID" };
        var yesOption = new Option<bool>("--yes") { Description = "Skip confirmation" };
        deleteCommand.Arguments.Add(deleteUuidArg);
        deleteCommand.Options.Add(yesOption);
        deleteCommand.SetHandler(async (string? workspace, string? repo, string environment, string variableUuid, bool yes) =>
        {
            if (!yes && !CommandRunner.ConfirmOrCancelStderr($"Delete deployment variable {variableUuid}? [y/N]: "))
                return;
            await CommandRunner.RunActionAsync(() =>
                services.GetRequiredService<DeleteDeploymentVariableHandler>()
                    .HandleAsync(new DeleteDeploymentVariableRequest(workspace, repo, environment, variableUuid), CommandBinding.CancellationToken));
        }, workspaceOption, repoOption, envOption, deleteUuidArg, yesOption);
        variablesCommand.Subcommands.Add(deleteCommand);

        return variablesCommand;
    }

}
