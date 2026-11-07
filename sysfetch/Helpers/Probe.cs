namespace sysfetch.Helpers;

/// <summary>
/// Runs independent info probes in parallel under a shared deadline. A probe that
/// throws or misses the deadline yields its fallback instead of blocking output.
/// </summary>
public static class Probe
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

    public static Task<T> Start<T>(Func<T> probe) => Task.Run(probe);

    public static void WaitAll(TimeSpan timeout, params Task[] tasks)
    {
        try
        {
            Task.WaitAll(tasks, timeout);
        }
        catch (AggregateException)
        {
            // Individual failures are handled in Result().
        }
    }

    public static T Result<T>(Task<T> task, T fallback) =>
        task.IsCompletedSuccessfully && task.Result is { } value ? value : fallback;
}
