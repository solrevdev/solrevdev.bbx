using Bbx.Auth;

namespace Bbx.Features.Common;

internal static class Resolve
{
    public static string Workspace(CredentialManager credentials, string? workspace, string errorMessage)
    {
        workspace ??= credentials.LoadConfig().DefaultWorkspace;
        if (string.IsNullOrEmpty(workspace))
            throw new BbxUserException(errorMessage);
        return workspace;
    }

    public static (string Workspace, string Repo) WorkspaceAndRepo(
        CredentialManager credentials,
        string? workspace,
        string? repo,
        string errorMessage)
    {
        workspace ??= credentials.LoadConfig().DefaultWorkspace;
        if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(repo))
            throw new BbxUserException(errorMessage);
        return (workspace, repo);
    }

    public static (string Workspace, string Repo) RepoPath(
        CredentialManager credentials,
        string? workspace,
        string path,
        string errorMessage)
    {
        if (path.Contains('/'))
        {
            var parts = path.Split('/', 2);
            return (parts[0], parts[1]);
        }

        workspace ??= credentials.LoadConfig().DefaultWorkspace;
        if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(path))
            throw new BbxUserException(errorMessage);
        return (workspace, path);
    }

    public static (string Workspace, string Repo) WorkspaceAndRepoFlexible(
        CredentialManager credentials,
        string? workspace,
        string? repo,
        string errorMessage)
    {
        var ws = workspace ?? credentials.LoadConfig().DefaultWorkspace;
        var repository = repo;
        if (!string.IsNullOrEmpty(repository) && repository.Contains('/'))
        {
            var parts = repository.Split('/', 2);
            ws = parts[0];
            repository = parts[1];
        }
        if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(repository))
            throw new BbxUserException(errorMessage);
        return (ws, repository);
    }
}
