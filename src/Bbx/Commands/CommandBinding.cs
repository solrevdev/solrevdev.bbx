using System.CommandLine;

namespace Bbx.Commands;

/// <summary>
/// A value that a handler wants read out of a parse result.
/// </summary>
/// <remarks>
/// System.CommandLine 2.0 reads values through <see cref="ParseResult.GetValue{T}(Option{T})"/>
/// and <see cref="ParseResult.GetValue{T}(Argument{T})"/>. Those are two
/// overloads with no common base that carries the value type. This wrapper closes over whichever
/// one applies, and the implicit conversions let a command pass an
/// <see cref="Option{T}"/> or an <see cref="Argument{T}"/> interchangeably to the
/// <c>SetHandler</c> overloads below.
/// </remarks>
internal readonly struct Bound<T>
{
    private readonly Func<ParseResult, T> _read;

    private Bound(Func<ParseResult, T> read) => _read = read;

    public T From(ParseResult parseResult) => _read(parseResult);

    // GetValue is annotated T? for every T, so the compiler cannot see that a
    // nullable result is only possible when T is itself nullable. The wrapper
    // carries T through unchanged: a command that wants a value it may not get
    // declares Option<string?>, and gets Bound<string?> here.
    public static implicit operator Bound<T>(Option<T> option) => new(pr => pr.GetValue(option)!);

    public static implicit operator Bound<T>(Argument<T> argument) => new(pr => pr.GetValue(argument)!);
}

/// <summary>
/// Binding helpers over System.CommandLine 2.0.
/// </summary>
/// <remarks>
/// 2.0 replaced the strongly-typed <c>SetHandler(handler, symbols…)</c> family
/// with a single <c>SetAction(ParseResult, CancellationToken)</c> callback,
/// leaving each command to pull its own values out. These overloads keep the
/// declarative shape, so the command files stay a description of the CLI rather
/// than a pile of <c>GetValue</c> calls. The compiler still checks that every
/// bound symbol matches its handler parameter.
/// </remarks>
internal static class CommandBinding
{
    private static readonly AsyncLocal<CancellationToken> ActiveCancellation = new();

    public static CancellationToken CancellationToken => ActiveCancellation.Value;

    /// <summary>
    /// Add an option that applies to this command and everything under it.
    /// </summary>
    public static void AddRecursiveOption(this Command command, Option option)
    {
        option.Recursive = true;
        command.Options.Add(option);
    }

    public static void SetHandler(this Command command, Func<Task> handler)
        => command.SetAction((_, ct) => InvokeAsync(handler, ct));

    public static void SetHandler<T1>(
        this Command command, Func<T1, Task> handler, Bound<T1> b1)
        => command.SetAction((pr, ct) => InvokeAsync(() => handler(b1.From(pr)), ct));

    public static void SetHandler<T1, T2>(
        this Command command, Func<T1, T2, Task> handler, Bound<T1> b1, Bound<T2> b2)
        => command.SetAction((pr, ct) => InvokeAsync(() => handler(b1.From(pr), b2.From(pr)), ct));

    public static void SetHandler<T1, T2, T3>(
        this Command command, Func<T1, T2, T3, Task> handler,
        Bound<T1> b1, Bound<T2> b2, Bound<T3> b3)
        => command.SetAction((pr, ct) => InvokeAsync(
            () => handler(b1.From(pr), b2.From(pr), b3.From(pr)), ct));

    public static void SetHandler<T1, T2, T3, T4>(
        this Command command, Func<T1, T2, T3, T4, Task> handler,
        Bound<T1> b1, Bound<T2> b2, Bound<T3> b3, Bound<T4> b4)
        => command.SetAction((pr, ct) => InvokeAsync(() => handler(
            b1.From(pr), b2.From(pr), b3.From(pr), b4.From(pr)), ct));

    public static void SetHandler<T1, T2, T3, T4, T5>(
        this Command command, Func<T1, T2, T3, T4, T5, Task> handler,
        Bound<T1> b1, Bound<T2> b2, Bound<T3> b3, Bound<T4> b4, Bound<T5> b5)
        => command.SetAction((pr, ct) => InvokeAsync(() => handler(
            b1.From(pr), b2.From(pr), b3.From(pr), b4.From(pr), b5.From(pr)), ct));

    public static void SetHandler<T1, T2, T3, T4, T5, T6>(
        this Command command, Func<T1, T2, T3, T4, T5, T6, Task> handler,
        Bound<T1> b1, Bound<T2> b2, Bound<T3> b3, Bound<T4> b4, Bound<T5> b5, Bound<T6> b6)
        => command.SetAction((pr, ct) => InvokeAsync(() => handler(
            b1.From(pr), b2.From(pr), b3.From(pr), b4.From(pr), b5.From(pr), b6.From(pr)), ct));

    public static void SetHandler<T1, T2, T3, T4, T5, T6, T7>(
        this Command command, Func<T1, T2, T3, T4, T5, T6, T7, Task> handler,
        Bound<T1> b1, Bound<T2> b2, Bound<T3> b3, Bound<T4> b4, Bound<T5> b5, Bound<T6> b6,
        Bound<T7> b7)
        => command.SetAction((pr, ct) => InvokeAsync(() => handler(
            b1.From(pr), b2.From(pr), b3.From(pr), b4.From(pr), b5.From(pr), b6.From(pr),
            b7.From(pr)), ct));

    public static void SetHandler<T1, T2, T3, T4, T5, T6, T7, T8>(
        this Command command, Func<T1, T2, T3, T4, T5, T6, T7, T8, Task> handler,
        Bound<T1> b1, Bound<T2> b2, Bound<T3> b3, Bound<T4> b4, Bound<T5> b5, Bound<T6> b6,
        Bound<T7> b7, Bound<T8> b8)
        => command.SetAction((pr, ct) => InvokeAsync(() => handler(
            b1.From(pr), b2.From(pr), b3.From(pr), b4.From(pr), b5.From(pr), b6.From(pr),
            b7.From(pr), b8.From(pr)), ct));

    private static async Task InvokeAsync(Func<Task> handler, CancellationToken cancellationToken)
    {
        var previous = ActiveCancellation.Value;
        ActiveCancellation.Value = cancellationToken;
        try
        {
            await handler();
        }
        finally
        {
            ActiveCancellation.Value = previous;
        }
    }
}
