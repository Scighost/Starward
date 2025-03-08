namespace Starward.RPC.GameInstall;

public enum GameInstallState
{

    /// <summary>
    /// 停止
    /// </summary>
    Stop = 0,

    /// <summary>
    /// 不确定进度的等待
    /// </summary>
    Waiting = 1,

    /// <summary>
    /// 下载中
    /// </summary>
    Downloading = 2,

    /// <summary>
    /// 解压中
    /// </summary>
    Decompressing = 3,

    /// <summary>
    /// 合并中
    /// </summary>
    Merging = 4,

    /// <summary>
    /// 验证中
    /// </summary>
    Verifying = 5,

    /// <summary>
    /// 已暂停
    /// </summary>
    Paused = 6,

    /// <summary>
    /// 完成
    /// </summary>
    Finish = 7,

    /// <summary>
    /// 错误
    /// </summary>
    Error = 8,

    /// <summary>
    /// 队列中
    /// </summary>
    Queueing = 9,

}
