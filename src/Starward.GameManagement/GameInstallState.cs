namespace Starward.GameManagement;

public enum GameInstallState
{
    /// <summary>
    /// 停止
    /// </summary>
    Stop = 0,

    /// <summary>
    /// 运行中
    /// </summary>
    Running = 1,

    /// <summary>
    /// 已暂停
    /// </summary>
    Paused = 2,

    /// <summary>
    /// 完成
    /// </summary>
    Finish = 3,

    /// <summary>
    /// 错误
    /// </summary>
    Error = 4,

    /// <summary>
    /// 队列中
    /// </summary>
    Queue = 5,
}
