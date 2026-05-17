using System.Net.Http.Headers;
using Bbx.Api;
using Bbx.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Composition;

public static class ServiceRegistration
{
    private const string BaseUrl = "https://api.bitbucket.org/2.0/";

    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddSingleton<HttpMessageHandler>(_ => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        });

        services.AddSingleton(sp =>
        {
            var client = new HttpClient(sp.GetRequiredService<HttpMessageHandler>(), disposeHandler: false)
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30),
            };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd("bbx-cli/1.0");
            return client;
        });

        services.AddSingleton<ICredentialStore, FileCredentialStore>();
        services.AddSingleton<CredentialManager>();

        services.AddSingleton<IBrowserLauncher, DefaultBrowserLauncher>();
        services.AddSingleton(sp => new OAuthFlow(
            sp.GetRequiredService<IBrowserLauncher>(),
            sp.GetRequiredService<HttpClient>()));
        services.AddSingleton(sp => new OAuthAuthProvider(
            sp.GetRequiredService<CredentialManager>(),
            sp.GetRequiredService<HttpClient>()));

        services.AddSingleton<ConfigAuthProvider>();
        services.AddSingleton<IAuthProvider>(sp => sp.GetRequiredService<ConfigAuthProvider>());

        services.AddSingleton(sp => new BitbucketClient(
            sp.GetRequiredService<HttpClient>(),
            sp.GetRequiredService<IAuthProvider>()));

        RegisterHandlers(services);

        return services.BuildServiceProvider();
    }

    private static void RegisterHandlers(IServiceCollection services)
    {
        services.AddTransient<Features.Auth.LoginApiToken.LoginApiTokenHandler>();
        services.AddTransient<Features.Auth.LoginOAuth.LoginOAuthHandler>();
        services.AddTransient<Features.Auth.LoginGuide.LoginGuideHandler>();
        services.AddTransient<Features.Auth.SetupOAuth.SetupOAuthHandler>();
        services.AddTransient<Features.Auth.Refresh.RefreshHandler>();
        services.AddTransient<Features.Auth.Status.AuthStatusHandler>();
        services.AddTransient<Features.Auth.Logout.LogoutHandler>();
        services.AddTransient<Features.Auth.Token.AuthTokenHandler>();
        services.AddTransient<Features.Auth.SetWorkspace.SetWorkspaceHandler>();

        services.AddTransient<Features.Repos.ListRepos.ListReposHandler>();
        services.AddTransient<Features.Repos.ViewRepo.ViewRepoHandler>();
        services.AddTransient<Features.Repos.CreateRepo.CreateRepoHandler>();
        services.AddTransient<Features.Repos.DeleteRepo.DeleteRepoHandler>();
        services.AddTransient<Features.Repos.ForkRepo.ForkRepoHandler>();
        services.AddTransient<Features.Repos.CloneRepo.CloneRepoHandler>();
        services.AddTransient<Features.Repos.RepoPermissions.RepoPermissionsHandler>();
        services.AddTransient<Features.Repos.Hooks.ListRepoHooks.ListRepoHooksHandler>();
        services.AddTransient<Features.Repos.Hooks.ViewRepoHook.ViewRepoHookHandler>();
        services.AddTransient<Features.Repos.Hooks.CreateRepoHook.CreateRepoHookHandler>();
        services.AddTransient<Features.Repos.Hooks.UpdateRepoHook.UpdateRepoHookHandler>();
        services.AddTransient<Features.Repos.Hooks.DeleteRepoHook.DeleteRepoHookHandler>();

        services.AddTransient<Features.Source.LsSource.LsSourceHandler>();
        services.AddTransient<Features.Source.CatSource.CatSourceHandler>();
        services.AddTransient<Features.Source.WriteSource.WriteSourceHandler>();

        services.AddTransient<Features.Tags.ListTags.ListTagsHandler>();
        services.AddTransient<Features.Tags.ViewTag.ViewTagHandler>();
        services.AddTransient<Features.Tags.CreateTag.CreateTagHandler>();
        services.AddTransient<Features.Tags.DeleteTag.DeleteTagHandler>();

        services.AddTransient<Features.PullRequests.ListPullRequests.ListPullRequestsHandler>();
        services.AddTransient<Features.PullRequests.ViewPullRequest.ViewPullRequestHandler>();
        services.AddTransient<Features.PullRequests.CreatePullRequest.CreatePullRequestHandler>();
        services.AddTransient<Features.PullRequests.MergePullRequest.MergePullRequestHandler>();
        services.AddTransient<Features.PullRequests.ApprovePullRequest.ApprovePullRequestHandler>();
        services.AddTransient<Features.PullRequests.UnapprovePullRequest.UnapprovePullRequestHandler>();
        services.AddTransient<Features.PullRequests.DeclinePullRequest.DeclinePullRequestHandler>();
        services.AddTransient<Features.PullRequests.ListPullRequestComments.ListPullRequestCommentsHandler>();
        services.AddTransient<Features.PullRequests.AddPullRequestComment.AddPullRequestCommentHandler>();
        services.AddTransient<Features.PullRequests.PullRequestDiff.PullRequestDiffHandler>();
        services.AddTransient<Features.PullRequests.PullRequestActivity.PullRequestActivityHandler>();
        services.AddTransient<Features.PullRequests.PullRequestStatuses.PullRequestStatusesHandler>();

        services.AddTransient<Features.Branches.ListBranches.ListBranchesHandler>();
        services.AddTransient<Features.Branches.ViewBranch.ViewBranchHandler>();
        services.AddTransient<Features.Branches.CreateBranch.CreateBranchHandler>();
        services.AddTransient<Features.Branches.DeleteBranch.DeleteBranchHandler>();
        services.AddTransient<Features.Branches.ListBranchRestrictions.ListBranchRestrictionsHandler>();
        services.AddTransient<Features.Branches.AddBranchRestriction.AddBranchRestrictionHandler>();
        services.AddTransient<Features.Branches.DeleteBranchRestriction.DeleteBranchRestrictionHandler>();

        services.AddTransient<Features.Commits.ListCommits.ListCommitsHandler>();
        services.AddTransient<Features.Commits.ViewCommit.ViewCommitHandler>();
        services.AddTransient<Features.Commits.CommitDiff.CommitDiffHandler>();
        services.AddTransient<Features.Commits.CommitPatch.CommitPatchHandler>();
        services.AddTransient<Features.Commits.ListCommitComments.ListCommitCommentsHandler>();
        services.AddTransient<Features.Commits.ListCommitStatuses.ListCommitStatusesHandler>();
        services.AddTransient<Features.Commits.ListCommitPullRequests.ListCommitPullRequestsHandler>();
        services.AddTransient<Features.Commits.CreateCommitStatus.CreateCommitStatusHandler>();
        services.AddTransient<Features.Commits.UpdateCommitStatus.UpdateCommitStatusHandler>();

        services.AddTransient<Features.Issues.ListIssues.ListIssuesHandler>();
        services.AddTransient<Features.Issues.ViewIssue.ViewIssueHandler>();
        services.AddTransient<Features.Issues.CreateIssue.CreateIssueHandler>();
        services.AddTransient<Features.Issues.UpdateIssue.UpdateIssueHandler>();
        services.AddTransient<Features.Issues.DeleteIssue.DeleteIssueHandler>();
        services.AddTransient<Features.Issues.ListIssueComments.ListIssueCommentsHandler>();
        services.AddTransient<Features.Issues.AddIssueComment.AddIssueCommentHandler>();

        services.AddTransient<Features.Pipelines.ListPipelines.ListPipelinesHandler>();
        services.AddTransient<Features.Pipelines.ViewPipeline.ViewPipelineHandler>();
        services.AddTransient<Features.Pipelines.TriggerPipeline.TriggerPipelineHandler>();
        services.AddTransient<Features.Pipelines.StopPipeline.StopPipelineHandler>();
        services.AddTransient<Features.Pipelines.PipelineLogs.PipelineLogsHandler>();
        services.AddTransient<Features.Pipelines.ListPipelineSteps.ListPipelineStepsHandler>();
        services.AddTransient<Features.Pipelines.ListPipelineVariables.ListPipelineVariablesHandler>();
        services.AddTransient<Features.Pipelines.AddPipelineVariable.AddPipelineVariableHandler>();
        services.AddTransient<Features.Pipelines.DeletePipelineVariable.DeletePipelineVariableHandler>();
        services.AddTransient<Features.Pipelines.ListPipelineSchedules.ListPipelineSchedulesHandler>();
        services.AddTransient<Features.Pipelines.CreatePipelineSchedule.CreatePipelineScheduleHandler>();
        services.AddTransient<Features.Pipelines.DeletePipelineSchedule.DeletePipelineScheduleHandler>();
        services.AddTransient<Features.Pipelines.ListPipelineCaches.ListPipelineCachesHandler>();
        services.AddTransient<Features.Pipelines.ClearPipelineCache.ClearPipelineCacheHandler>();
        services.AddTransient<Features.Pipelines.ListDeploymentEnvironments.ListDeploymentEnvironmentsHandler>();
        services.AddTransient<Features.Pipelines.ViewDeploymentEnvironment.ViewDeploymentEnvironmentHandler>();

        services.AddTransient<Features.Snippets.ListSnippets.ListSnippetsHandler>();
        services.AddTransient<Features.Snippets.ViewSnippet.ViewSnippetHandler>();
        services.AddTransient<Features.Snippets.CreateSnippet.CreateSnippetHandler>();
        services.AddTransient<Features.Snippets.UpdateSnippet.UpdateSnippetHandler>();
        services.AddTransient<Features.Snippets.DeleteSnippet.DeleteSnippetHandler>();
        services.AddTransient<Features.Snippets.SnippetFiles.SnippetFilesHandler>();
        services.AddTransient<Features.Snippets.SnippetWatch.SnippetWatchHandler>();
        services.AddTransient<Features.Snippets.SnippetComments.SnippetCommentsHandler>();

        services.AddTransient<Features.Workspaces.ListWorkspaces.ListWorkspacesHandler>();
        services.AddTransient<Features.Workspaces.ViewWorkspace.ViewWorkspaceHandler>();
        services.AddTransient<Features.Workspaces.ListWorkspaceMembers.ListWorkspaceMembersHandler>();
        services.AddTransient<Features.Workspaces.WorkspaceProjects.WorkspaceProjectsHandler>();
        services.AddTransient<Features.Workspaces.ListWorkspacePermissions.ListWorkspacePermissionsHandler>();
        services.AddTransient<Features.Workspaces.WorkspaceHooks.WorkspaceHooksHandler>();
    }
}
