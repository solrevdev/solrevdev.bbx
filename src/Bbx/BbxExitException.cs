namespace Bbx;

/// <summary>
/// Ends the run with a specific exit code after the command has already printed
/// its JSON. Used when the output is useful but the job is not done, such as a
/// merge still queued when the wait ran out.
/// </summary>
public sealed class BbxExitException(int exitCode, string message) : Exception(message)
{
    public int ExitCode { get; } = exitCode;
}
