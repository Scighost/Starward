namespace Starward.Setup.Services;

public static class RetryHelper
{


    public static int DefaultRetryCount { get; set; } = 2;


    public static int DefaultDelay { get; set; } = 200;


    public static async ValueTask<T> ExecuteAsync<T>(Func<CancellationToken, ValueTask<T>> func, CancellationToken cancellation = default)
    {
        return await ExecuteAsync(func, DefaultRetryCount, DefaultDelay, cancellation).ConfigureAwait(false);
    }


    public static async ValueTask ExecuteAsync(Func<CancellationToken, ValueTask> func, CancellationToken cancellation = default)
    {
        await ExecuteAsync(func, DefaultRetryCount, DefaultDelay, cancellation).ConfigureAwait(false);
    }


    public static async ValueTask<T> ExecuteAsync<T>(Func<CancellationToken, ValueTask<T>> func, int retryCount, int delay, CancellationToken cancellation = default)
    {
        int count = 0;
        while (true)
        {
            try
            {
                return await func(cancellation).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (count++ >= retryCount)
                {
                    throw;
                }
                await Task.Delay(delay, cancellation).ConfigureAwait(false);
            }
        }
    }


    public static async ValueTask ExecuteAsync(Func<CancellationToken, ValueTask> func, int retryCount, int delay, CancellationToken cancellation = default)
    {
        int count = 0;
        while (true)
        {
            try
            {
                await func(cancellation).ConfigureAwait(false);
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (count++ >= retryCount)
                {
                    throw;
                }
                await Task.Delay(delay, cancellation).ConfigureAwait(false);
            }
        }
    }


}




