using Starward.Core;
using Starward.Core.HoYoPlay;

namespace Starward.GameManagement;

public class GameInstallProgress
{
    public LauncherConfig LauncherConfig { get; set; }

    public GameId GameId { get; set; }

    public string InstallPath { get; set; }

    public GameInstallOperation Operation { get; set; }

    public AudioLanguage AudioLanguage { get; set; }

    public long Timestamp { get; set; }

    public int CurrentStep { get; set; }

    public int TotalStep { get; set; }

    public GameInstallStep Step { get; set; }

    public GameInstallState State { get; set; }

    public string? ErrorMessage { get; set; }


    /// <summary>
    /// 当前阶段进度百分比，最大值1
    /// </summary>
    public double PercentProgress { get; set; }


    /// <summary>
    /// 当前阶段需要展示的进度信息
    /// </summary>
    public ProgressDisplay ProgressDisplay { get; set; }


    /// <summary>
    /// 需要下载的总字节数
    /// </summary>
    public long DownloadTotalBytes { get; set; }
    /// <summary>
    /// 已下载的字节数
    /// </summary>
    public long DownloadFinishedBytes { get; set; }

    /// <summary>
    /// 需要写入的总字节数
    /// </summary>
    public long WriteTotalBytes { get; set; }
    /// <summary>
    /// 已写入的字节数
    /// </summary>
    public long WriteFinishedBytes { get; set; }

    /// <summary>
    /// 硬链接字节数
    /// </summary>
    public long HardLinkBytes { get; set; }


    /// <summary>
    /// 网络下载速度，单位是字节每秒
    /// </summary>
    public long NetworkDownloadSpeed { get; set; }

    /// <summary>
    /// 存储读取速度，单位是字节每秒
    /// </summary>
    public long StorageReadSpeed { get; set; }

    /// <summary>
    /// 存储写入速度，单位是字节每秒
    /// </summary>
    public long StorageWriteSpeed { get; set; }

    /// <summary>
    /// 预计剩余时间，仅用于预计下载剩余时间，单位是秒
    /// </summary>
    public long RemainTimeSeconds { get; set; }



    public static GameInstallProgress FromDTO(GameInstallProgressDTO dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new GameInstallProgress
        {
            LauncherConfig = new LauncherConfig(dto.LauncherId, dto.Channel, dto.SubChannel, dto.Host),
            GameId = new GameId { Id = dto.GameId, GameBiz = dto.GameBiz },
            InstallPath = dto.InstallPath,
            Operation = (GameInstallOperation)dto.Operation,
            AudioLanguage = (AudioLanguage)dto.AudioLanguage,
            Timestamp = dto.Timestamp,
            CurrentStep = dto.CurrentStep,
            TotalStep = dto.TotalStep,
            Step = (GameInstallStep)dto.Step,
            State = (GameInstallState)dto.State,
            ErrorMessage = dto.ErrorMessage,
            PercentProgress = dto.PercentProgress,
            ProgressDisplay = (ProgressDisplay)dto.ProgressDisplay,
            DownloadTotalBytes = dto.DownloadTotalBytes,
            DownloadFinishedBytes = dto.DownloadFinishedBytes,
            WriteTotalBytes = dto.WriteTotalBytes,
            WriteFinishedBytes = dto.WriteFinishedBytes,
            HardLinkBytes = dto.HardLinkBytes,
            NetworkDownloadSpeed = dto.NetworkDownloadSpeed,
            RemainTimeSeconds = dto.RemainTimeSeconds,
        };
    }


    public static GameInstallProgress FromContext(GameInstallContext ctx)
    {
        return new GameInstallProgress
        {
            LauncherConfig = ctx.LauncherConfig,
            GameId = ctx.GameId,
            InstallPath = ctx.InstallPath,
            Operation = ctx.Operation,
            AudioLanguage = ctx.AudioLanguage,
            Timestamp = ctx.Timestamp,
            CurrentStep = ctx.CurrentStep,
            TotalStep = ctx.TotalStep,
            Step = ctx.Step,
            State = ctx.State,
            ErrorMessage = ctx.ErrorMessage,
            PercentProgress = ctx.PercentProgress,
            ProgressDisplay = ctx.ProgressDisplay,
            DownloadTotalBytes = ctx.DownloadTotalBytes,
            DownloadFinishedBytes = ctx.DownloadFinishedBytes,
            WriteTotalBytes = ctx.WriteTotalBytes,
            WriteFinishedBytes = ctx.WriteFinishedBytes,
            HardLinkBytes = ctx.HardLinkBytes,
            NetworkDownloadSpeed = ctx.NetworkSpeed,
            RemainTimeSeconds = ctx.RemainTimeSeconds,
        };
    }



    public GameInstallProgressDTO ToDTO()
    {
        return new GameInstallProgressDTO
        {
            LauncherId = LauncherConfig.Id,
            Channel = LauncherConfig.Channel,
            SubChannel = LauncherConfig.SubChannel,
            Host = LauncherConfig.Host,
            GameBiz = GameId.GameBiz.Value,
            GameId = GameId.Id,
            InstallPath = InstallPath,
            Operation = (int)Operation,
            AudioLanguage = (int)AudioLanguage,
            Timestamp = Timestamp,
            CurrentStep = CurrentStep,
            TotalStep = TotalStep,
            Step = (int)Step,
            State = (int)State,
            ErrorMessage = ErrorMessage,
            PercentProgress = PercentProgress,
            ProgressDisplay = (int)ProgressDisplay,
            DownloadTotalBytes = DownloadTotalBytes,
            DownloadFinishedBytes = DownloadFinishedBytes,
            WriteTotalBytes = WriteTotalBytes,
            WriteFinishedBytes = WriteFinishedBytes,
            HardLinkBytes = HardLinkBytes,
            NetworkDownloadSpeed = NetworkDownloadSpeed,
            RemainTimeSeconds = RemainTimeSeconds,
        };
    }



    public void Update(GameInstallProgressDTO dto)
    {
        Timestamp = dto.Timestamp;
        CurrentStep = dto.CurrentStep;
        TotalStep = dto.TotalStep;
        Step = (GameInstallStep)dto.Step;
        State = (GameInstallState)dto.State;
        ErrorMessage = dto.ErrorMessage;
        PercentProgress = dto.PercentProgress;
        ProgressDisplay = (ProgressDisplay)dto.ProgressDisplay;
        DownloadTotalBytes = dto.DownloadTotalBytes;
        DownloadFinishedBytes = dto.DownloadFinishedBytes;
        WriteTotalBytes = dto.WriteTotalBytes;
        WriteFinishedBytes = dto.WriteFinishedBytes;
        HardLinkBytes = dto.HardLinkBytes;
        NetworkDownloadSpeed = dto.NetworkDownloadSpeed;
        RemainTimeSeconds = dto.RemainTimeSeconds;
    }

}