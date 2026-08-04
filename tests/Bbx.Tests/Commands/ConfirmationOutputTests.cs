using AwesomeAssertions;
using Bbx.Commands;
using Bbx.Tests.TestKit;
using Microsoft.Extensions.DependencyInjection;

namespace Bbx.Tests.Commands;

[Collection("Console")]
public class ConfirmationOutputTests
{
    [Theory]
    [InlineData("repo")]
    [InlineData("branch")]
    [InlineData("issue")]
    public async Task Destructive_prompts_do_not_write_to_stdout(string group)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var command = group switch
        {
            "repo" => RepoCommand.Create(services),
            "branch" => BranchCommand.Create(services),
            _ => IssueCommand.Create(services),
        };
        var args = group switch
        {
            "repo" => new[] { "delete", "ws/repo" },
            "branch" => new[] { "delete", "feature" },
            _ => new[] { "delete", "1" },
        };
        var originalInput = Console.In;
        Console.SetIn(new StringReader("n\n"));
        try
        {
            var (stdout, stderr) = await CaptureConsole.RunAsync(
                () => command.Parse(args).InvokeAsync());

            stdout.Should().BeEmpty();
            stderr.Should().Contain("[y/N]").And.Contain("Cancelled.");
        }
        finally
        {
            Console.SetIn(originalInput);
        }
    }
}
