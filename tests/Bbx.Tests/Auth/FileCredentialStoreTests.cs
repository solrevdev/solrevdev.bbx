using AwesomeAssertions;
using Bbx.Auth;

namespace Bbx.Tests.Auth;

public sealed class FileCredentialStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"bbx-credential-tests-{Guid.NewGuid():N}");

    [Fact]
    public void Save_writes_a_private_file_and_leaves_no_temporary_file()
    {
        var path = Path.Combine(_directory, "config.json");
        var store = new FileCredentialStore(_directory, path);

        store.Save(new BbxConfig
        {
            Username = "jane@example.com",
            ApiToken = "secret",
        });

        store.Load().ApiToken.Should().Be("secret");
        Directory.GetFiles(_directory).Should().Equal(path);
        if (!OperatingSystem.IsWindows())
        {
            File.GetUnixFileMode(path).Should().Be(
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.GetUnixFileMode(_directory).Should().Be(
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [Fact]
    public void Load_reports_a_corrupt_config_instead_of_treating_it_as_logged_out()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "config.json");
        File.WriteAllText(path, "{not-json");
        var store = new FileCredentialStore(_directory, path);

        var act = store.Load;

        act.Should().Throw<BbxUserException>().WithMessage("*Could not read bbx config*");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
