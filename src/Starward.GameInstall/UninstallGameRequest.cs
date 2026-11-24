using Starward.Core;
using Starward.Core.HoYoPlay;

namespace Starward.GameInstall;

public class UninstallGameRequest
{
    public LauncherConfig LauncherConfig { get; set; }

    public GameId GameId { get; set; }

    public string InstallPath { get; set; }

    public string? ScreenshotPath { get; set; }

    public string? ScreenshotBackupPath { get; set; }


    public static UninstallGameRequest FromDTO(UninstallGameRequestDTO dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new UninstallGameRequest
        {
            LauncherConfig = new LauncherConfig(dto.GameId.LauncherId, dto.GameId.Channel, dto.GameId.SubChannel, (ServerRegion)dto.GameId.Region, dto.GameId.IsBeta),
            GameId = new GameId { Id = dto.GameId.GameId, GameBiz = dto.GameId.GameBiz },
            InstallPath = dto.InstallPath,
            ScreenshotPath = dto.ScreenshotPath,
            ScreenshotBackupPath = dto.ScreenshotBackupPath,
        };
    }


    public UninstallGameRequestDTO ToDTO()
    {
        return new UninstallGameRequestDTO
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
            ScreenshotPath = ScreenshotPath,
            ScreenshotBackupPath = ScreenshotBackupPath,
        };
    }

}
