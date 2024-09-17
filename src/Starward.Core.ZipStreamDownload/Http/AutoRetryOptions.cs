namespace Starward.Core.ZipStreamDownload.Http;

/// <summary>
/// 自动重试选项
/// </summary>
public class AutoRetryOptions
{
    /// <summary>
    /// 当网络错误时可允许的最大重试次数
    /// <remarks>取值范围(0,20)，默认10</remarks>
    /// </summary>
    public int RetryTimesOnNetworkError
    {
        get => _retryTimesOnNetworkError;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 20);
            _retryTimesOnNetworkError = value;
        }
    }

    /// <summary>
    /// （内部）当网络错误时可允许的最大重试次数
    /// </summary>
    private int _retryTimesOnNetworkError = 10;

    /// <summary>
    /// 自动重试等待时间（单位：毫秒）
    /// <remarks>取值范围(0,2000)，默认1000</remarks>
    /// </summary>
    public int DelayMillisecond
    {
        get => _delayMillisecond;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 20);
            _delayMillisecond = value;
        }
    }

    /// <summary>
    /// （内部）自动重试等待时间（单位：毫秒）
    /// </summary>
    private int _delayMillisecond = 1000;
}