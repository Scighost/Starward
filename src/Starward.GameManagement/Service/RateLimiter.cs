using System.Threading.RateLimiting;

namespace Starward.GameManagement.Service;

internal static class RateLimiter
{

    private static TokenBucketRateLimiter _limiter;

    public static bool EnableLimit { get; set; }

    public static int BytesPerSecond { get; private set; }


    static RateLimiter()
    {
        SetLimit(int.MaxValue);
    }


    public static int SetLimit(int bytesPerSec)
    {
        var oldLimiter = _limiter;
        int limit = Math.Clamp(bytesPerSec / 10, DownloadService.BUFFER_SIZE, int.MaxValue);
        _limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            AutoReplenishment = true,
            QueueLimit = int.MaxValue,
            TokenLimit = limit,
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(100),
            TokensPerPeriod = limit,
        });
        BytesPerSecond = Math.Clamp(limit, 0, int.MaxValue / 10) * 10;
        oldLimiter.Dispose();
        return BytesPerSecond;
    }


    public static async ValueTask AcquireAsync(int bytes, CancellationToken cancellation = default)
    {
        if (!EnableLimit)
        {
            return;
        }
        using RateLimitLease lease = await _limiter.AcquireAsync(bytes, cancellation).ConfigureAwait(false);
        if (!lease.IsAcquired)
        {
            throw new InvalidOperationException("Rate limit acquire failed.");
        }
    }

}