using Starward.Core;
using Starward.Core.HoYoPlay;

namespace Starward.GameInstall;

public class GameInstallProgress
{
    public LauncherConfig LauncherConfig { get; set; }

    public GameId GameId { get; set; }

    public string InstallPath { get; set; }

    public GameInstallOperation Operation { get; set; }

    public AudioLanguage AudioLanguage { get; set; }

    public string CategoryScenario { get; set; }

    public long Timestamp { get; set; }

    public int CurrentStep { get; set; }

    public int TotalStep { get; set; }

    public RunnerStep Step { get; set; }

    public RunnerState State { get; set; }

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
    /// 需要从网络下载的总字节数
    /// </summary>
    public long NetworkTotalBytes { get; set; }
    /// <summary>
    /// 已从网络下载的字节数
    /// </summary>
    public long NetworkFinishedBytes { get; set; }

    /// <summary>
    /// 需要写入存储的总字节数
    /// </summary>
    public long StorageTotalBytes { get; set; }
    /// <summary>
    /// 已写入存储的字节数
    /// </summary>
    public long StorageFinishedBytes { get; set; }

    /// <summary>
    /// 硬链接字节数
    /// </summary>
    public long HardLinkBytes { get; set; }


    /// <summary>
    /// 网络下载速度，单位是字节每秒
    /// </summary>
    public long NetworkDownloadSpeed { get; set; }

    /// <summary>
    /// 预计剩余时间，仅用于预计下载剩余时间，单位是秒
    /// </summary>
    public long RemainTimeSeconds { get; set; }



    internal static GameInstallProgress FromContext(GameInstallContext ctx)
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
            NetworkTotalBytes = ctx.NetworkTotalBytes,
            NetworkFinishedBytes = ctx.NetworkFinishedBytes,
            StorageTotalBytes = ctx.StorageTotalBytes,
            StorageFinishedBytes = ctx.StorageFinishedBytes,
            HardLinkBytes = ctx.HardLinkBytes,
            NetworkDownloadSpeed = ctx.NetworkSpeed,
            RemainTimeSeconds = ctx.RemainTimeSeconds,
            CategoryScenario = ctx.CategoryScenario,
        };
    }



    public static GameInstallProgress FromDTO(GameInstallProgressDTO dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new GameInstallProgress()
        {
            LauncherConfig = new LauncherConfig(dto.GameId.LauncherId, dto.GameId.Channel, dto.GameId.SubChannel, (ServerRegion)dto.GameId.Region, dto.GameId.IsBeta),
            GameId = new GameId { Id = dto.GameId.GameId, GameBiz = dto.GameId.GameBiz },
            InstallPath = dto.InstallPath,
            Operation = (GameInstallOperation)dto.Operation,
            AudioLanguage = (AudioLanguage)dto.AudioLanguage,
            CategoryScenario = dto.CategoryScenario,
            Timestamp = dto.Timestamp,
            CurrentStep = dto.CurrentStep,
            TotalStep = dto.TotalStep,
            Step = (RunnerStep)dto.Step,
            State = (RunnerState)dto.State,
            ErrorMessage = dto.ErrorMessage,
            ProgressDisplay = (ProgressDisplay)dto.ProgressDisplay,
            PercentProgress = dto.PercentProgress,
            NetworkTotalBytes = dto.NetworkTotalBytes,
            NetworkFinishedBytes = dto.NetworkFinishedBytes,
            StorageTotalBytes = dto.StorageTotalBytes,
            StorageFinishedBytes = dto.StorageFinishedBytes,
            HardLinkBytes = dto.HardLinkBytes,
            NetworkDownloadSpeed = dto.NetworkDownloadSpeed,
            RemainTimeSeconds = dto.RemainTimeSeconds,
        };
    }


    public GameInstallProgressDTO ToDTO()
    {
        return new GameInstallProgressDTO
        {
            GameId = new GameIdDTO
            {
                Region = (int)LauncherConfig.Region,
                LauncherId = LauncherConfig.Id,
                Channel = LauncherConfig.Channel,
                SubChannel = LauncherConfig.SubChannel,
                IsBeta = LauncherConfig.IsBeta,
                GameBiz = GameId.GameBiz.Value,
                GameId = GameId.Id,
            },
            InstallPath = InstallPath,
            Operation = (int)Operation,
            AudioLanguage = (int)AudioLanguage,
            CategoryScenario = CategoryScenario,
            Timestamp = Timestamp,
            CurrentStep = CurrentStep,
            TotalStep = TotalStep,
            Step = (int)Step,
            State = (int)State,
            ErrorMessage = ErrorMessage,
            ProgressDisplay = (int)ProgressDisplay,
            PercentProgress = PercentProgress,
            NetworkTotalBytes = NetworkTotalBytes,
            NetworkFinishedBytes = NetworkFinishedBytes,
            StorageTotalBytes = StorageTotalBytes,
            StorageFinishedBytes = StorageFinishedBytes,
            HardLinkBytes = HardLinkBytes,
            NetworkDownloadSpeed = NetworkDownloadSpeed,
            RemainTimeSeconds = RemainTimeSeconds,
        };
    }



}