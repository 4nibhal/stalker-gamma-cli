namespace Stalker.Gamma.Filesystem;

public sealed class UnsafePathException : IOException
{
    public UnsafePathException(string message, Exception? inner = null)
        : base(message, inner) { }

    public static bool IsUnsafe(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (error is UnsafePathException)
        {
            return true;
        }
        if (error is AggregateException aggregate)
        {
            return aggregate.InnerExceptions.Any(IsUnsafe);
        }
        return error.InnerException is { } inner && IsUnsafe(inner);
    }
}
