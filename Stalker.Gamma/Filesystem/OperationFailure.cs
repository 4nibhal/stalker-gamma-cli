namespace Stalker.Gamma.Filesystem;

public static class OperationFailure
{
    public static bool IsCancellation(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (error is OperationCanceledException)
        {
            return true;
        }
        if (error is AggregateException aggregate)
        {
            return aggregate.InnerExceptions.Any(IsCancellation);
        }
        return error.InnerException is { } inner && IsCancellation(inner);
    }
}
