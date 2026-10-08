namespace Starward.GameManagement;

public enum GameInstallStep
{
    None = 0,

    /// <summary>
    /// 等待中
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 下载中
    /// </summary>
    Downloading = 2,

    /// <summary>
    /// 校验中
    /// </summary>
    Verifying = 3,

    /// <summary>
    /// 合并中
    /// </summary>
    Patching = 4,

    /// <summary>
    /// 解压中
    /// </summary>
    Decompressing = 5,

    /// <summary>
    /// 清理中
    /// </summary>
    Cleaning = 6,

}
