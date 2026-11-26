using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Core.Passport;

namespace Starward.GameInstall;


public class GameInstallRequest
{
    public LauncherConfig LauncherConfig { get; set; }

    public GameId GameId { get; set; }

    public string? HardLinkPath { get; set; }

    public string InstallPath { get; set; }

    public GameInstallOperation Operation { get; set; }

    public string CategoryScenario { get; set; }

    public AudioLanguage AudioLanguage { get; set; }

    public PassportUserInfo? PassportUserInfo { get; set; }

    public List<string> CutsceneBlockList { get; set; }


    internal GameInstallContext ToContext()
    {
        return new GameInstallContext
        {
            LauncherConfig = LauncherConfig,
            GameId = GameId,
            HardLinkPath = HardLinkPath,
            InstallPath = Path.GetFullPath(InstallPath),
            Operation = Operation,
            CategoryScenario = CategoryScenario ?? Core.HoYoPlay.CategoryScenario.CATEGORY_SCENARIO_FULL,
            AudioLanguage = AudioLanguage,
            CutsceneBlockList = CutsceneBlockList
        };
    }



    public static GameInstallRequest FromDTO(GameInstallRequestDTO dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new GameInstallRequest
        {
            LauncherConfig = new LauncherConfig(dto.GameId.LauncherId, dto.GameId.Channel, dto.GameId.SubChannel, (ServerRegion)dto.GameId.Region, dto.GameId.IsBeta),
            GameId = new GameId { Id = dto.GameId.GameId, GameBiz = dto.GameId.GameBiz },
            HardLinkPath = dto.HardLinkPath,
            InstallPath = dto.InstallPath,
            Operation = (GameInstallOperation)dto.Operation,
            CategoryScenario = dto.CategoryScenario,
            AudioLanguage = (AudioLanguage)dto.AudioLanguage,
            PassportUserInfo = dto.Passport is null ? null : new PassportUserInfo
            {
                Region = (ServerRegion)dto.GameId.Region,
                Aid = dto.Passport.Aid,
                Mid = dto.Passport.Mid,
                Stoken = dto.Passport.Stoken,
            },
            CutsceneBlockList = dto.CutsceneBlockList.ToList(),
        };
    }


    public GameInstallRequestDTO ToDTO()
    {
        var dto = new GameInstallRequestDTO
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
            HardLinkPath = HardLinkPath,
            InstallPath = InstallPath,
            Operation = (int)Operation,
            AudioLanguage = (int)AudioLanguage,
            CategoryScenario = CategoryScenario,
            Passport = PassportUserInfo is null ? null : new PassportUserInfoDTO
            {
                Aid = PassportUserInfo.Aid,
                Mid = PassportUserInfo.Mid,
                Stoken = PassportUserInfo.Stoken,
            },
        };
        if (CutsceneBlockList is not null)
        {
            dto.CutsceneBlockList.AddRange(CutsceneBlockList);
        }
        return dto;
    }

}
