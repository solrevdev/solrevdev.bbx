namespace Bbx.Features.Common;

/// <summary>
/// A handler result that carries an exit code. <c>CommandRunner</c> prints
/// <see cref="Output"/> as JSON, then ends the run with <see cref="ExitCode"/>
/// and writes <see cref="Message"/> to stderr when the code is not zero.
/// </summary>
public sealed record CommandOutcome(object? Output, int ExitCode = 0, string? Message = null);

public static class ExitCodes
{
    /// <summary>
    /// sysexits EX_TEMPFAIL: not failed, not finished, try again later. A merge
    /// still queued when the wait runs out ends with this so a script can tell
    /// it from a merge that failed (1).
    /// </summary>
    public const int Pending = 75;
}
