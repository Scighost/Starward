using Starward.Core;
using Starward.Core.HoYoPlay;

namespace Starward.GameManagement;


public class GameInstallRequest
{
    public LauncherConfig LauncherConfig { get; set; }

    public GameId GameId { get; set; }

    public string InstallPath { get; set; }

    public GameInstallOperation Operation { get; set; }

    public string CategoryScenario { get; set; }

    public AudioLanguage AudioLanguage { get; set; }

    public bool ForceStart { get; set; }


    public GameInstallRequestDTO ToDTO()
    {
        return new GameInstallRequestDTO
        {
            LauncherId = LauncherConfig.Id,
            Channel = LauncherConfig.Channel,
            SubChannel = LauncherConfig.SubChannel,
            Host = LauncherConfig.Host,
            GameBiz = GameId.GameBiz.Value,
            GameId = GameId.Id,
            InstallPath = InstallPath,
            Operation = (int)Operation,
            CategoryScenario = CategoryScenario,
            AudioLanguage = (int)AudioLanguage,
        };
    }


    public static GameInstallRequest FromDTO(GameInstallRequestDTO dto)
    {
        return new GameInstallRequest
        {
            LauncherConfig = new LauncherConfig(dto.LauncherId, dto.Channel, dto.SubChannel, dto.Host),
            GameId = new GameId
            {
                Id = dto.GameId,
                GameBiz = dto.GameBiz,
            },
            InstallPath = dto.InstallPath,
            Operation = (GameInstallOperation)dto.Operation,
            CategoryScenario = dto.CategoryScenario,
            AudioLanguage = (AudioLanguage)dto.AudioLanguage,
        };
    }


    public GameInstallContext ToContext()
    {
        return new GameInstallContext
        {
            LauncherConfig = LauncherConfig,
            GameId = GameId,
            InstallPath = Path.GetFullPath(InstallPath),
            Operation = Operation,
            CategoryScenario = CategoryScenario ?? Core.HoYoPlay.CategoryScenario.CATEGORY_SCENARIO_FULL,
            AudioLanguage = AudioLanguage,
        };
    }

}