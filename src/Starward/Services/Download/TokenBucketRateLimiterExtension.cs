using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Runtime.CompilerServices;

namespace Starward.Services.Download;

internal static class TokenBucketRateLimiterExtension
{
    public static bool TryAcquire(this TokenBucketRateLimiter rateLimiter, int permits, out int acquired, out TimeSpan retryAfter)
    {
        acquired = Math.Min(permits, (int)Volatile.Read(ref PrivateGetTokenCount(rateLimiter)));
        lock (PrivateGetLock(rateLimiter))
            return !rateLimiter.AttemptAcquire(acquired).TryGetMetadata(MetadataName.RetryAfter, out retryAfter);
    }

    // private object Lock → _queue
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Lock")]
    private static extern object PrivateGetLock(TokenBucketRateLimiter rateLimiter);

    // private double _tokenCount;
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_tokenCount")]
    private static extern ref double PrivateGetTokenCount(TokenBucketRateLimiter rateLimiter);
}
