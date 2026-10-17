using System.Runtime.ExceptionServices;

namespace LinuxRegression;

internal static class Check
{
    internal static void True(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    internal static void Equal<T>(T expected, T actual, string message = "") =>
        True(EqualityComparer<T>.Default.Equals(expected, actual),
            $"Expected <{expected}>; actual <{actual}>. {message}");

    internal static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = "")
    {
        var expectedItems = expected.ToArray();
        var actualItems = actual.ToArray();
        True(expectedItems.SequenceEqual(actualItems),
            $"Sequences differ (expected {expectedItems.Length}, actual {actualItems.Length} items). {message}");
    }

    internal static T Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T error)
        {
            return error;
        }
        catch (Exception error)
        {
            throw new InvalidOperationException($"Expected {typeof(T).Name}; got {error.GetType().Name}.", error);
        }
        throw new InvalidOperationException($"Expected {typeof(T).Name}; no exception was thrown.");
    }

    internal static async Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try
        {
            await action();
        }
        catch (T error)
        {
            return error;
        }
        catch (Exception error)
        {
            throw new InvalidOperationException($"Expected {typeof(T).Name}; got {error.GetType().Name}.", error);
        }
        throw new InvalidOperationException($"Expected {typeof(T).Name}; no exception was thrown.");
    }

    // Test-only body/disposal wrappers. Ordinary using/finally would replace a
    // body exception if disposal also threw; retain both, in that order.
    internal static void WithCleanup(Action body, Action cleanup)
    {
        Exception? bodyFailure = null;
        try
        {
            body();
        }
        catch (Exception error)
        {
            bodyFailure = error;
        }
        CompleteCleanup(bodyFailure, cleanup);
    }

    internal static async Task WithCleanupAsync(Func<Task> body, Action cleanup)
    {
        Exception? bodyFailure = null;
        try
        {
            await body();
        }
        catch (Exception error)
        {
            bodyFailure = error;
        }
        CompleteCleanup(bodyFailure, cleanup);
    }

    private static void CompleteCleanup(Exception? bodyFailure, Action cleanup)
    {
        try
        {
            cleanup();
        }
        catch (Exception cleanupFailure) when (bodyFailure is not null)
        {
            throw new AggregateException(bodyFailure, cleanupFailure);
        }
        if (bodyFailure is not null)
            ExceptionDispatchInfo.Capture(bodyFailure).Throw();
    }
}
