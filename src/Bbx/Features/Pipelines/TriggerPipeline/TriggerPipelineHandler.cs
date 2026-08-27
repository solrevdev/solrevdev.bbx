using System.Text.Json;
using Bbx.Api;
using Bbx.Auth;
using Bbx.Features.Common;

namespace Bbx.Features.Pipelines.TriggerPipeline;

public sealed class TriggerPipelineHandler(BitbucketClient client, CredentialManager credentials)
{
    public const string CommitNeedsBranch =
        "Error: --commit needs --branch. Bitbucket does not check that the commit is on the branch, "
        + "so it would run against whichever branch was guessed.";

    public const string BranchAndPullRequest =
        "Error: --branch cannot be combined with --pull-request. Both branches and both commits "
        + "come from the pull request; a run against any other branch is not a pull-request run.";

    public const string OnDemandFlagsNeedYaml =
        "Error: --merge-defaults and --target-branch-to-create only apply to an on-demand run. "
        + "Pass --yaml with them.";

    public const string EmptyYamlPath =
        "Error: --yaml was given an empty path. Pass a YAML file, or '-' for stdin.";

    public const string StdinYamlNeedsAPipe =
        "Error: --yaml - reads the pipeline YAML from stdin, but stdin is a terminal. "
        + "Pipe the YAML in, or pass a file path.";

    public async Task<object> HandleAsync(TriggerPipelineRequest request, CancellationToken ct)
    {
        var (ws, repository) = Resolve.WorkspaceAndRepoFlexible(credentials, request.Workspace, request.Repository,
            "Workspace and repository are required.");

        // Bitbucket accepts a commit that is not on the branch you name and runs
        // it anyway, labelled with that branch, so a guessed branch beside an
        // explicit commit is silently wrong rather than rejected.
        if (!string.IsNullOrEmpty(request.Commit) && string.IsNullOrWhiteSpace(request.Branch))
            throw new BbxUserException(CommitNeedsBranch);

        // An empty --yaml is a broken invocation (an unset shell variable, say),
        // not a request for a config-file run. Routing it to the normal trigger
        // would start a real pipeline the caller never asked for.
        if (request.YamlPath is not null && string.IsNullOrWhiteSpace(request.YamlPath))
            throw new BbxUserException(EmptyYamlPath);

        if (request.YamlPath is null
            && (request.MergeDefaults || !string.IsNullOrEmpty(request.TargetBranchToCreate)))
        {
            throw new BbxUserException(OnDemandFlagsNeedYaml);
        }

        // Read the YAML before any network call, so a typo'd path fails on its
        // own error rather than after an authenticated round trip.
        var yaml = request.YamlPath is null ? null : ReadYaml(request.YamlPath);

        var target = !string.IsNullOrEmpty(request.PullRequestId)
            ? await PullRequestTargetAsync(ws, repository, request, ct)
            : await BranchTargetAsync(ws, repository, request, ct);

        var variables = ParseVariables(request.Variables);

        var pipeline = yaml is null
            ? await TriggerAsync(ws, repository, target, variables, ct)
            : await TriggerOnDemandAsync(ws, repository, target, variables, request, yaml, ct);

        return new
        {
            message = "Pipeline triggered successfully",
            pipeline = PipelineFormat.Pipeline(pipeline),
        };
    }

    private async Task<JsonElement> TriggerAsync(
        string ws, string repository, Dictionary<string, object> target,
        List<PipelineVariable> variables, CancellationToken ct)
    {
        var body = new Dictionary<string, object> { ["target"] = target };
        if (variables.Count > 0)
        {
            body["variables"] = variables
                .Select(v => new { key = v.Key, value = v.Value, secured = v.Secured })
                .ToList();
        }

        return await client.PostAsync<JsonElement>($"repositories/{ws}/{repository}/pipelines/", body, ct);
    }

    /// <summary>
    /// Trigger an on-demand pipeline: the supplied YAML is the body and applies
    /// only to this run, overriding <c>bitbucket-pipelines.yml</c>.
    /// </summary>
    /// <remarks>
    /// Because the body carries the YAML, the target and variables move into
    /// query parameters, keyed by the JSON path of the field they replace
    /// (<c>target.ref_name</c>, <c>variables[0].key</c>, …). That shape comes
    /// from the endpoint's own documentation, not the spec's parameter list,
    /// which only names <c>merge_defaults</c> and <c>target_branch_to_create</c>.
    /// </remarks>
    private async Task<JsonElement> TriggerOnDemandAsync(
        string ws, string repository, Dictionary<string, object> target,
        List<PipelineVariable> variables, TriggerPipelineRequest request, string yaml, CancellationToken ct)
    {
        var query = new List<KeyValuePair<string, string>>();
        Flatten("target", target, query);
        for (var i = 0; i < variables.Count; i++)
        {
            query.Add(new($"variables[{i}].key", variables[i].Key));
            query.Add(new($"variables[{i}].value", variables[i].Value));
            query.Add(new($"variables[{i}].secured", variables[i].Secured ? "true" : "false"));
        }
        if (request.MergeDefaults)
            query.Add(new("merge_defaults", "true"));
        if (!string.IsNullOrEmpty(request.TargetBranchToCreate))
            query.Add(new("target_branch_to_create", request.TargetBranchToCreate));

        var queryString = string.Join("&", query.Select(p =>
            $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        return await client.PostRawAsync<JsonElement>(
            $"repositories/{ws}/{repository}/pipelines/?{queryString}", yaml, "application/yaml", ct);
    }

    private static string ReadYaml(string path)
    {
        if (path == "-")
        {
            // Reading a terminal blocks forever with no prompt, which is the
            // hang BBX_NO_INTERACTIVE exists to prevent; a pipe or redirect is
            // the only sane source here.
            if (!Console.IsInputRedirected)
                throw new BbxUserException(StdinYamlNeedsAPipe);
            return Console.In.ReadToEnd();
        }

        if (!File.Exists(path))
            throw new BbxUserException($"Error: YAML file not found: {path}");
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Flatten the nested target into query-parameter form: each leaf becomes
    /// the JSON path of the field (<c>target.commit.hash</c>).
    /// </summary>
    private static void Flatten(string prefix, object value, List<KeyValuePair<string, string>> into)
    {
        switch (value)
        {
            case Dictionary<string, object> nested:
                foreach (var (key, child) in nested)
                    Flatten($"{prefix}.{key}", child, into);
                break;
            case string leaf:
                into.Add(new(prefix, leaf));
                break;
            default:
                // A silent ToString would send "True" or an anonymous type's
                // debug text as a query value, and Bitbucket 201-accepts bad
                // on-demand requests, so the breakage would only surface as a
                // failed run minutes later.
                throw new InvalidOperationException(
                    $"Pipeline target leaf '{prefix}' is a {value?.GetType().Name ?? "null"}; "
                    + "target values must be strings or nested dictionaries.");
        }
    }

    private sealed record PipelineVariable(string Key, string Value, bool Secured);

    private static List<PipelineVariable> ParseVariables(string[] variables) => variables
        .Select(v => v.Split('=', 2))
        .Where(parts => parts.Length == 2)
        .Select(parts => new PipelineVariable(
            parts[0], parts[1], parts[0].Contains("SECRET", StringComparison.OrdinalIgnoreCase)))
        .ToList();

    /// <summary>
    /// Build the target for a pull-request run.
    /// </summary>
    /// <remarks>
    /// This is a real <c>pipeline_pullrequest_target</c>, and it has to carry
    /// both branches and both commits. A <c>pipeline_ref_target</c> on the
    /// source branch with a <c>pull-requests</c> selector does run the steps
    /// under <c>pull-requests:</c>, which is why it looked right, but the run is
    /// not a pull-request run: <c>BITBUCKET_PR_ID</c> and
    /// <c>BITBUCKET_PR_DESTINATION_BRANCH</c> are absent from it entirely, so a
    /// script reading either gets an empty string. Both were read out of a live
    /// build's log on 2026-08-07, empty for the ref target and populated here.
    /// The id may be a string or a number. <c>selector</c> is the only optional
    /// field.
    /// </remarks>
    private async Task<Dictionary<string, object>> PullRequestTargetAsync(
        string ws, string repository, TriggerPipelineRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Branch))
            throw new BbxUserException(BranchAndPullRequest);

        var refs = await PullRequestRefs.ReadAsync(client, ws, repository, request.PullRequestId!, ct);

        var target = new Dictionary<string, object>
        {
            ["type"] = "pipeline_pullrequest_target",
            ["source"] = refs.Source,
            ["destination"] = refs.Destination,
            ["commit"] = new Dictionary<string, object> { ["hash"] = refs.SourceCommit },
            ["destination_commit"] = new Dictionary<string, object> { ["hash"] = refs.DestinationCommit },
            ["pullrequest"] = new Dictionary<string, object> { ["id"] = request.PullRequestId! },
        };

        if (!string.IsNullOrEmpty(request.Pattern))
        {
            target["selector"] = new Dictionary<string, object>
            {
                ["type"] = "pull-requests",
                ["pattern"] = request.Pattern,
            };
        }

        return target;
    }

    private async Task<Dictionary<string, object>> BranchTargetAsync(
        string ws, string repository, TriggerPipelineRequest request, CancellationToken ct)
    {
        // Without --branch, take the repository's own main branch rather than
        // assume "main".
        var branch = await DefaultBranch.ResolveAsync(client, ws, repository, request.Branch, ct);

        var target = new Dictionary<string, object>
        {
            ["type"] = "pipeline_ref_target",
            ["ref_type"] = "branch",
            ["ref_name"] = branch,
        };

        if (!string.IsNullOrEmpty(request.Commit))
            target["commit"] = new Dictionary<string, object> { ["hash"] = request.Commit };
        if (!string.IsNullOrEmpty(request.Pattern))
        {
            target["selector"] = new Dictionary<string, object>
            {
                ["type"] = "custom",
                ["pattern"] = request.Pattern,
            };
        }

        return target;
    }
}
