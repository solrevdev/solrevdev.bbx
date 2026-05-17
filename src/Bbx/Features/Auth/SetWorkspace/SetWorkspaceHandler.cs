using Bbx.Auth;

namespace Bbx.Features.Auth.SetWorkspace;

public sealed class SetWorkspaceHandler(CredentialManager credentials)
{
    public Task<string> HandleAsync(SetWorkspaceRequest request, CancellationToken ct)
    {
        var config = credentials.LoadConfig();
        config.DefaultWorkspace = request.Workspace;
        credentials.SaveConfig(config);
        return Task.FromResult($"✓ Default workspace set to: {request.Workspace}");
    }
}
