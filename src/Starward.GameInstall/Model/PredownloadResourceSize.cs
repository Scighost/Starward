namespace Starward.GameInstall;


/// <summary>
/// 预下载的资源用量
/// </summary>
/// <param name="DownloadSize">需要从网络下载的字节数</param>
/// <param name="SpaceRequiredSize">下载与应用期间需要的磁盘空间</param>
public readonly record struct PredownloadResourceSize(long DownloadSize, long SpaceRequiredSize);
