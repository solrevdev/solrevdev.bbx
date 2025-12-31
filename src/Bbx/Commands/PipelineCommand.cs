using System.CommandLine;
using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;

namespace Bbx.Commands;

public static class PipelineCommand
{
    public static Command Create()
    {
        var command = new Command("pipeline", "Manage Bitbucket Pipelines");

        command.AddCommand(CreateListCommand());
        command.AddCommand(CreateViewCommand());
        command.AddCommand(CreateTriggerCommand());
        command.AddCommand(CreateStopCommand());
        command.AddCommand(CreateLogsCommand());
        command.AddCommand(CreateStepsCommand());
        command.AddCommand(CreateVariablesCommand());
        command.AddCommand(CreateSchedulesCommand());
        command.AddCommand(CreateCachesCommand());
        command.AddCommand(CreateDeploymentsCommand());

        return command;
    }

    private static Command CreateListCommand()
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

        command.SetHandler(async (workspace, repo, status, sort, limit, branch) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required. Use --workspace and --repo or set defaults.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);
            var pipelines = new List<JsonElement>();

            try
            {
                var query = BuildPipelineQuery(status, branch);
                var url = $"repositories/{ws}/{repository}/pipelines/?sort={sort}";
                if (!string.IsNullOrEmpty(query))
                {
                    url += $"&q={Uri.EscapeDataString(query)}";
                }

                await foreach (var pipeline in client.GetPaginatedAsync<JsonElement>(url))
                {
                    pipelines.Add(pipeline);
                    if (pipelines.Count >= limit) break;
                }

                var output = new
                {
                    workspace = ws,
                    repository,
                    count = pipelines.Count,
                    pipelines = pipelines.Select(p => FormatPipeline(p))
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error listing pipelines: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, statusOption, sortOption, limitOption, targetBranchOption);

        return command;
    }

    private static Command CreateViewCommand()
    {
        var command = new Command("view", "View details of a specific pipeline");

        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var pipelineArg = new Argument<string>("pipeline-uuid", "Pipeline UUID");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddArgument(pipelineArg);

        command.SetHandler(async (workspace, repo, pipelineUuid) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                var pipeline = await client.GetAsync<JsonElement>($"repositories/{ws}/{repository}/pipelines/{pipelineUuid}");
                var steps = await client.GetAsync<JsonElement>($"repositories/{ws}/{repository}/pipelines/{pipelineUuid}/steps/");

                var output = new
                {
                    pipeline = FormatPipelineDetailed(pipeline),
                    steps = steps.TryGetProperty("values", out var stepsArray)
                        ? stepsArray.EnumerateArray().Select(FormatStep).ToList()
                        : new List<object>()
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error viewing pipeline: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, pipelineArg);

        return command;
    }

    private static Command CreateTriggerCommand()
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

        command.SetHandler(async (workspace, repo, branch, commit, pattern, variables) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                var target = new Dictionary<string, object>
                {
                    ["type"] = "pipeline_ref_target",
                    ["ref_type"] = "branch",
                    ["ref_name"] = branch
                };

                if (!string.IsNullOrEmpty(commit))
                {
                    target["commit"] = new { hash = commit };
                }

                if (!string.IsNullOrEmpty(pattern))
                {
                    target["selector"] = new { type = "custom", pattern };
                }

                var body = new Dictionary<string, object> { ["target"] = target };

                if (variables.Length > 0)
                {
                    var pipelineVars = variables
                        .Select(v => v.Split('=', 2))
                        .Where(parts => parts.Length == 2)
                        .Select(parts => new { key = parts[0], value = parts[1], secured = parts[0].Contains("SECRET", StringComparison.OrdinalIgnoreCase) })
                        .ToList();

                    body["variables"] = pipelineVars;
                }

                var pipeline = await client.PostAsync<JsonElement>($"repositories/{ws}/{repository}/pipelines/", body);

                var output = new
                {
                    message = "Pipeline triggered successfully",
                    pipeline = FormatPipeline(pipeline)
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error triggering pipeline: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, branchOption, commitOption, patternOption, variablesOption);

        return command;
    }

    private static Command CreateStopCommand()
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

        command.SetHandler(async (workspace, repo, pipelineUuid, yes) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            if (!yes)
            {
                Console.Error.Write($"Stop pipeline {pipelineUuid}? [y/N]: ");
                var response = Console.ReadLine();
                if (!response?.Equals("y", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Console.Error.WriteLine("Cancelled.");
                    return;
                }
            }

            var client = new BitbucketClient(credentials);

            try
            {
                await client.PostAsync<JsonElement>($"repositories/{ws}/{repository}/pipelines/{pipelineUuid}/stopPipeline", new { });

                var output = new
                {
                    message = "Pipeline stop requested",
                    pipeline_uuid = pipelineUuid
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error stopping pipeline: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, pipelineArg, yesOption);

        return command;
    }

    private static Command CreateLogsCommand()
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

        command.SetHandler(async (workspace, repo, pipelineUuid, stepUuid, follow) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                var log = await client.GetRawAsync($"repositories/{ws}/{repository}/pipelines/{pipelineUuid}/steps/{stepUuid}/log");

                var output = new
                {
                    pipeline_uuid = pipelineUuid,
                    step_uuid = stepUuid,
                    log
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error fetching logs: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, pipelineArg, stepArg, followOption);

        return command;
    }

    private static Command CreateStepsCommand()
    {
        var command = new Command("steps", "List steps for a pipeline");

        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var pipelineArg = new Argument<string>("pipeline-uuid", "Pipeline UUID");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddArgument(pipelineArg);

        command.SetHandler(async (workspace, repo, pipelineUuid) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                var steps = new List<JsonElement>();
                await foreach (var step in client.GetPaginatedAsync<JsonElement>($"repositories/{ws}/{repository}/pipelines/{pipelineUuid}/steps/"))
                {
                    steps.Add(step);
                }

                var output = new
                {
                    pipeline_uuid = pipelineUuid,
                    count = steps.Count,
                    steps = steps.Select(FormatStep)
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error listing steps: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, pipelineArg);

        return command;
    }

    private static Command CreateVariablesCommand()
    {
        var command = new Command("variables", "Manage pipeline variables");

        command.AddCommand(CreateVariablesListCommand());
        command.AddCommand(CreateVariablesAddCommand());
        command.AddCommand(CreateVariablesDeleteCommand());

        return command;
    }

    private static Command CreateVariablesListCommand()
    {
        var command = new Command("list", "List pipeline variables");

        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);

        command.SetHandler(async (workspace, repo) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                var variables = new List<JsonElement>();
                await foreach (var variable in client.GetPaginatedAsync<JsonElement>($"repositories/{ws}/{repository}/pipelines_config/variables/"))
                {
                    variables.Add(variable);
                }

                var output = new
                {
                    workspace = ws,
                    repository,
                    count = variables.Count,
                    variables = variables.Select(v => new
                    {
                        uuid = GetStringProperty(v, "uuid"),
                        key = GetStringProperty(v, "key"),
                        secured = v.TryGetProperty("secured", out var sec) && sec.GetBoolean(),
                        value = v.TryGetProperty("secured", out var s) && s.GetBoolean() ? "***" : GetStringProperty(v, "value")
                    })
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error listing variables: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption);

        return command;
    }

    private static Command CreateVariablesAddCommand()
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

        command.SetHandler(async (workspace, repo, key, value, secured) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                var body = new
                {
                    type = "pipeline_variable",
                    key,
                    value,
                    secured
                };

                var variable = await client.PostAsync<JsonElement>($"repositories/{ws}/{repository}/pipelines_config/variables/", body);

                var output = new
                {
                    message = "Variable added successfully",
                    variable = new
                    {
                        uuid = GetStringProperty(variable, "uuid"),
                        key = GetStringProperty(variable, "key"),
                        secured
                    }
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error adding variable: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, keyOption, valueOption, securedOption);

        return command;
    }

    private static Command CreateVariablesDeleteCommand()
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

        command.SetHandler(async (workspace, repo, uuid, yes) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            if (!yes)
            {
                Console.Error.Write($"Delete variable {uuid}? [y/N]: ");
                var response = Console.ReadLine();
                if (!response?.Equals("y", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Console.Error.WriteLine("Cancelled.");
                    return;
                }
            }

            var client = new BitbucketClient(credentials);

            try
            {
                await client.DeleteAsync($"repositories/{ws}/{repository}/pipelines_config/variables/{uuid}");

                var output = new
                {
                    message = "Variable deleted successfully",
                    uuid
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deleting variable: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, uuidArg, yesOption);

        return command;
    }

    private static Command CreateSchedulesCommand()
    {
        var command = new Command("schedules", "Manage pipeline schedules");

        command.AddCommand(CreateSchedulesListCommand());
        command.AddCommand(CreateSchedulesCreateCommand());
        command.AddCommand(CreateSchedulesDeleteCommand());

        return command;
    }

    private static Command CreateSchedulesListCommand()
    {
        var command = new Command("list", "List pipeline schedules");

        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);

        command.SetHandler(async (workspace, repo) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                var schedules = new List<JsonElement>();
                await foreach (var schedule in client.GetPaginatedAsync<JsonElement>($"repositories/{ws}/{repository}/pipelines_config/schedules/"))
                {
                    schedules.Add(schedule);
                }

                var output = new
                {
                    workspace = ws,
                    repository,
                    count = schedules.Count,
                    schedules = schedules.Select(s => new
                    {
                        uuid = GetStringProperty(s, "uuid"),
                        enabled = s.TryGetProperty("enabled", out var en) && en.GetBoolean(),
                        cron_pattern = GetStringProperty(s, "cron_pattern"),
                        target = s.TryGetProperty("target", out var t) ? new
                        {
                            ref_name = GetStringProperty(t, "ref_name"),
                            ref_type = GetStringProperty(t, "ref_type")
                        } : null,
                        created_on = GetStringProperty(s, "created_on")
                    })
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error listing schedules: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption);

        return command;
    }

    private static Command CreateSchedulesCreateCommand()
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

        command.SetHandler(async (workspace, repo, cron, branch, pattern, enabled) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                var target = new Dictionary<string, object>
                {
                    ["ref_type"] = "branch",
                    ["ref_name"] = branch
                };

                if (!string.IsNullOrEmpty(pattern))
                {
                    target["selector"] = new { type = "custom", pattern };
                }

                var body = new
                {
                    type = "pipeline_schedule",
                    enabled,
                    cron_pattern = cron,
                    target
                };

                var schedule = await client.PostAsync<JsonElement>($"repositories/{ws}/{repository}/pipelines_config/schedules/", body);

                var output = new
                {
                    message = "Schedule created successfully",
                    schedule = new
                    {
                        uuid = GetStringProperty(schedule, "uuid"),
                        cron_pattern = cron,
                        enabled,
                        branch
                    }
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error creating schedule: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, cronOption, branchOption, patternOption, enabledOption);

        return command;
    }

    private static Command CreateSchedulesDeleteCommand()
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

        command.SetHandler(async (workspace, repo, uuid, yes) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            if (!yes)
            {
                Console.Error.Write($"Delete schedule {uuid}? [y/N]: ");
                var response = Console.ReadLine();
                if (!response?.Equals("y", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Console.Error.WriteLine("Cancelled.");
                    return;
                }
            }

            var client = new BitbucketClient(credentials);

            try
            {
                await client.DeleteAsync($"repositories/{ws}/{repository}/pipelines_config/schedules/{uuid}");

                var output = new
                {
                    message = "Schedule deleted successfully",
                    uuid
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deleting schedule: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, uuidArg, yesOption);

        return command;
    }

    private static Command CreateCachesCommand()
    {
        var command = new Command("caches", "Manage pipeline caches");

        command.AddCommand(CreateCachesListCommand());
        command.AddCommand(CreateCachesClearCommand());

        return command;
    }

    private static Command CreateCachesListCommand()
    {
        var command = new Command("list", "List pipeline caches");

        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);

        command.SetHandler(async (workspace, repo) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                var caches = new List<JsonElement>();
                await foreach (var cache in client.GetPaginatedAsync<JsonElement>($"repositories/{ws}/{repository}/pipelines-config/caches/"))
                {
                    caches.Add(cache);
                }

                var output = new
                {
                    workspace = ws,
                    repository,
                    count = caches.Count,
                    caches = caches.Select(c => new
                    {
                        uuid = GetStringProperty(c, "uuid"),
                        name = GetStringProperty(c, "name"),
                        created_on = GetStringProperty(c, "created_on"),
                        file_size_bytes = c.TryGetProperty("file_size_bytes", out var fs) ? fs.GetInt64() : 0
                    })
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error listing caches: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption);

        return command;
    }

    private static Command CreateCachesClearCommand()
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

        command.SetHandler(async (workspace, repo, name, yes) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            if (!yes)
            {
                Console.Error.Write($"Clear cache '{name}'? [y/N]: ");
                var response = Console.ReadLine();
                if (!response?.Equals("y", StringComparison.OrdinalIgnoreCase) == true)
                {
                    Console.Error.WriteLine("Cancelled.");
                    return;
                }
            }

            var client = new BitbucketClient(credentials);

            try
            {
                await client.DeleteAsync($"repositories/{ws}/{repository}/pipelines-config/caches/{Uri.EscapeDataString(name)}");

                var output = new
                {
                    message = "Cache cleared successfully",
                    name
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error clearing cache: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, nameArg, yesOption);

        return command;
    }

    private static Command CreateDeploymentsCommand()
    {
        var command = new Command("deployments", "Manage deployment environments");

        command.AddCommand(CreateDeploymentsListCommand());
        command.AddCommand(CreateDeploymentsViewCommand());

        return command;
    }

    private static Command CreateDeploymentsListCommand()
    {
        var command = new Command("list", "List deployment environments");

        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);

        command.SetHandler(async (workspace, repo) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                var environments = new List<JsonElement>();
                await foreach (var env in client.GetPaginatedAsync<JsonElement>($"repositories/{ws}/{repository}/environments/"))
                {
                    environments.Add(env);
                }

                var output = new
                {
                    workspace = ws,
                    repository,
                    count = environments.Count,
                    environments = environments.Select(e => new
                    {
                        uuid = GetStringProperty(e, "uuid"),
                        name = GetStringProperty(e, "name"),
                        environment_type = e.TryGetProperty("environment_type", out var et) ? GetStringProperty(et, "name") : null,
                        rank = e.TryGetProperty("rank", out var r) ? r.GetInt32() : 0,
                        deployment_gate_enabled = e.TryGetProperty("deployment_gate_enabled", out var dg) && dg.GetBoolean()
                    })
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error listing environments: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption);

        return command;
    }

    private static Command CreateDeploymentsViewCommand()
    {
        var command = new Command("view", "View deployment environment details");

        var workspaceOption = new Option<string?>("--workspace", "Workspace slug");
        var repoOption = new Option<string?>("--repo", "Repository slug");
        var envArg = new Argument<string>("environment", "Environment UUID or name");

        command.AddOption(workspaceOption);
        command.AddOption(repoOption);
        command.AddArgument(envArg);

        command.SetHandler(async (workspace, repo, environment) =>
        {
            var credentials = CredentialManager.Load();
            if (credentials == null)
            {
                Console.Error.WriteLine("Not authenticated. Run 'bbx auth login' first.");
                Environment.ExitCode = 1;
                return;
            }

            var (ws, repository) = ResolveWorkspaceRepo(workspace, repo, credentials);
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            {
                Console.Error.WriteLine("Workspace and repository are required.");
                Environment.ExitCode = 1;
                return;
            }

            var client = new BitbucketClient(credentials);

            try
            {
                var env = await client.GetAsync<JsonElement>($"repositories/{ws}/{repository}/environments/{environment}");

                var output = new
                {
                    uuid = GetStringProperty(env, "uuid"),
                    name = GetStringProperty(env, "name"),
                    environment_type = env.TryGetProperty("environment_type", out var et) ? new
                    {
                        name = GetStringProperty(et, "name"),
                        rank = et.TryGetProperty("rank", out var r) ? r.GetInt32() : 0
                    } : null,
                    deployment_gate_enabled = env.TryGetProperty("deployment_gate_enabled", out var dg) && dg.GetBoolean(),
                    lock_ = env.TryGetProperty("lock", out var l) ? new
                    {
                        type = GetStringProperty(l, "type"),
                        name = GetStringProperty(l, "name")
                    } : null,
                    restrictions = env.TryGetProperty("restrictions", out var res) ? res : (object?)null
                };

                Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error viewing environment: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }, workspaceOption, repoOption, envArg);

        return command;
    }

    // Helper methods
    private static (string? workspace, string? repo) ResolveWorkspaceRepo(string? workspace, string? repo, Credentials credentials)
    {
        var ws = workspace ?? credentials.DefaultWorkspace;
        var repository = repo ?? credentials.DefaultRepository;

        // Handle workspace/repo format
        if (!string.IsNullOrEmpty(repository) && repository.Contains('/'))
        {
            var parts = repository.Split('/', 2);
            ws = parts[0];
            repository = parts[1];
        }

        return (ws, repository);
    }

    private static string BuildPipelineQuery(string? status, string? branch)
    {
        var conditions = new List<string>();

        if (!string.IsNullOrEmpty(status))
        {
            conditions.Add($"state.name=\"{status.ToUpperInvariant()}\"");
        }

        if (!string.IsNullOrEmpty(branch))
        {
            conditions.Add($"target.ref_name=\"{branch}\"");
        }

        return string.Join(" AND ", conditions);
    }

    private static object FormatPipeline(JsonElement p)
    {
        return new
        {
            uuid = GetStringProperty(p, "uuid"),
            build_number = p.TryGetProperty("build_number", out var bn) ? bn.GetInt32() : 0,
            state = p.TryGetProperty("state", out var state) ? new
            {
                name = GetStringProperty(state, "name"),
                result = state.TryGetProperty("result", out var r) ? GetStringProperty(r, "name") : null
            } : null,
            target = p.TryGetProperty("target", out var target) ? new
            {
                ref_type = GetStringProperty(target, "ref_type"),
                ref_name = GetStringProperty(target, "ref_name"),
                commit = target.TryGetProperty("commit", out var c) ? GetStringProperty(c, "hash") : null
            } : null,
            trigger = p.TryGetProperty("trigger", out var trigger) ? GetStringProperty(trigger, "name") : null,
            created_on = GetStringProperty(p, "created_on"),
            completed_on = GetStringProperty(p, "completed_on"),
            duration_in_seconds = p.TryGetProperty("duration_in_seconds", out var dur) ? dur.GetInt32() : (int?)null
        };
    }

    private static object FormatPipelineDetailed(JsonElement p)
    {
        var basic = FormatPipeline(p);
        return new
        {
            ((dynamic)basic).uuid,
            ((dynamic)basic).build_number,
            ((dynamic)basic).state,
            ((dynamic)basic).target,
            ((dynamic)basic).trigger,
            ((dynamic)basic).created_on,
            ((dynamic)basic).completed_on,
            ((dynamic)basic).duration_in_seconds,
            creator = p.TryGetProperty("creator", out var creator) ? new
            {
                display_name = GetStringProperty(creator, "display_name"),
                account_id = GetStringProperty(creator, "account_id")
            } : null,
            repository = p.TryGetProperty("repository", out var repo) ? new
            {
                name = GetStringProperty(repo, "name"),
                full_name = GetStringProperty(repo, "full_name")
            } : null,
            links = p.TryGetProperty("links", out var links) && links.TryGetProperty("html", out var html) 
                ? GetStringProperty(html, "href") : null
        };
    }

    private static object FormatStep(JsonElement s)
    {
        return new
        {
            uuid = GetStringProperty(s, "uuid"),
            name = GetStringProperty(s, "name"),
            state = s.TryGetProperty("state", out var state) ? new
            {
                name = GetStringProperty(state, "name"),
                result = state.TryGetProperty("result", out var r) ? GetStringProperty(r, "name") : null
            } : null,
            started_on = GetStringProperty(s, "started_on"),
            completed_on = GetStringProperty(s, "completed_on"),
            duration_in_seconds = s.TryGetProperty("duration_in_seconds", out var dur) ? dur.GetInt32() : (int?)null,
            run_number = s.TryGetProperty("run_number", out var rn) ? rn.GetInt32() : (int?)null,
            max_time = s.TryGetProperty("max_time", out var mt) ? mt.GetInt32() : (int?)null
        };
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }
}
