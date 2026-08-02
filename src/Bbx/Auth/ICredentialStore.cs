namespace Bbx.Auth;

public interface ICredentialStore
{
    BbxConfig Load();
    void Save(BbxConfig config);
    void Clear();
}
