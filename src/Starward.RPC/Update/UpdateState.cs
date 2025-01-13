namespace Starward.RPC.Update;

/// <summary>
/// 更新状态
/// </summary>
public enum UpdateState
{
    /// <summary>
    /// 停止
    /// </summary>
    Stop = 0,

    /// <summary>
    /// 不确定进度的等待
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 下载中
    /// </summary>
    Downloading = 2,

    /// <summary>
    /// 完成
    /// </summary>
    Finish = 3,

    /// <summary>
    /// 错误
    /// </summary>
    Error = 4,

    /// <summary>
    /// 不支持
    /// </summary>
    NotSupport = 5,
}
