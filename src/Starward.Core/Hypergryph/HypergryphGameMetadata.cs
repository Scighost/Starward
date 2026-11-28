using Starward.Core;
using Starward.Core.HoYoPlay;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Starward.Core.Hypergryph;

public static class HypergryphGameMetadata
{
    public static GameInfo CreateGameInfo(GameId gameId, HypergryphLauncherContent? launcherContent = null)
    {
        HypergryphGameProfile profile = HypergryphGameConstants.GetGameProfile(gameId);
        string? regionLogo = gameId.GameBiz.Value switch
        {
            GameBiz.endfield_cn or GameBiz.endfield_channel => HypergryphGameConstants.HypergryphLogo,
            GameBiz.endfield_bilibili => HypergryphGameConstants.BilibiliLogo,
            GameBiz.endfield_global => HypergryphGameConstants.GryphlineLogo,
            _ => null,
        };
        string? mainBackground = launcherContent?.MainBackground?.Url;
        string? background = launcherContent?.Artwork?.BackgroundUrl;
        string? cover = launcherContent?.Artwork?.CoverUrl;
        if (string.IsNullOrWhiteSpace(background))
        {
            background = mainBackground;
        }
        return new GameInfo
        {
            Id = gameId.Id,
            GameBiz = gameId.GameBiz,
            DisplayStatus = GameInfoDisplayStatus.LAUNCHER_GAME_DISPLAY_STATUS_AVAILABLE,
            GameServerConfigs = [],
            Display = new GameInfoDisplay
            {
                Language = CultureInfo.CurrentUICulture.Name,
                Name = gameId.GameBiz.ToGameName(),
                Title = gameId.GameBiz.ToGameName(),
                Subtitle = "",
                Icon = new GameImage { Url = profile.Icon },
                Logo = new GameImage { Url = profile.Logo },
                TopLeftLogo = regionLogo is null ? null : new GameImage { Url = regionLogo },
                Thumbnail = new GameImage { Url = string.IsNullOrWhiteSpace(cover) ? background ?? "" : cover },
                Background = new GameImage { Url = background ?? "" },
            },
        };
    }

    public static GameConfig CreateGameConfig(GameId gameId)
    {
        HypergryphGameProfile profile = HypergryphGameConstants.GetGameProfile(gameId);
        return new GameConfig
        {
            GameId = gameId,
            ExeFileName = profile.ExeName,
            InstallationDir = profile.InstallationDirectory,
            DefaultDownloadMode = DownloadMode.DOWNLOAD_MODE_FILE,
            RelatedProcesses = [.. profile.RelatedProcesses],
            RedundantFileCleanupPaths = [],
        };
    }

    public static GameBackgroundInfo CreateBackgroundInfo(GameId gameId, HypergryphLauncherContent? launcherContent)
    {
        List<GameBackground> backgrounds = [];
        HypergryphMainBackground? mainBackground = launcherContent?.MainBackground;
        if (!string.IsNullOrWhiteSpace(mainBackground?.Url))
        {
            bool isVideo = !string.IsNullOrWhiteSpace(mainBackground.VideoUrl);
            backgrounds.Add(new GameBackground
            {
                Id = string.IsNullOrWhiteSpace(mainBackground.MD5) ? mainBackground.Url : mainBackground.MD5,
                Background = new GameImage { Url = mainBackground.Url },
                Theme = new GameImage { Url = "" },
                Video = isVideo ? new GameImage { Url = mainBackground.VideoUrl } : null!,
                Type = isVideo ? GameBackground.BACKGROUND_TYPE_VIDEO : GameBackground.BACKGROUND_TYPE_UNSPECIFIED,
            });
        }
        return new GameBackgroundInfo
        {
            GameId = gameId,
            Backgrounds = backgrounds,
        };
    }

    public static GameContent CreateGameContent(GameId gameId, HypergryphLauncherContent launcherContent)
    {
        return new GameContent
        {
            GameId = gameId,
            Language = CultureInfo.CurrentUICulture.Name,
            Banners = launcherContent.Banners
                .Where(x => !string.IsNullOrWhiteSpace(x.Url))
                .Select(x => new GameBanner
                {
                    Id = x.Id,
                    Image = new GameImage
                    {
                        Url = x.Url,
                        Link = x.JumpUrl,
                        LoginStateInLink = x.NeedToken,
                    },
                }).ToList(),
            Posts = launcherContent.AnnouncementTabs
                .SelectMany(tab => tab.Announcements
                    .Where(x => !string.IsNullOrWhiteSpace(x.Content))
                    .Select(x => new GamePost
                    {
                        Id = x.Id,
                        Type = tab.TabName,
                        Title = x.Content,
                        Link = x.JumpUrl,
                        Date = FormatAnnouncementDate(x.StartTimestamp),
                    }))
                .ToList(),
            SocialMediaList = [],
        };
    }

    private static string FormatAnnouncementDate(string timestamp)
    {
        if (long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out long milliseconds))
        {
            try
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).ToLocalTime().ToString("MM/dd", CultureInfo.InvariantCulture);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }
        return "";
    }

    public static GamePackage CreateGamePackage(GameId gameId, HypergryphLatestGame latest)
    {
        HypergryphPackagePart firstPart = latest.Package.Packs.FirstOrDefault() ?? new();
        return new GamePackage
        {
            GameId = gameId,
            Main = new GamePackageVersion
            {
                Major = new GamePackageResource
                {
                    Version = latest.Version,
                    GamePackages =
                    [
                        new GamePackageFile
                        {
                            Url = firstPart.Url,
                            MD5 = firstPart.MD5,
                            Size = latest.Package.Packs.Sum(x => x.PackageSize),
                            DecompressedSize = latest.Package.TotalSize + latest.Package.Packs.Sum(x => x.PackageSize),
                        },
                    ],
                    AudioPackages = [],
                    ResListUrl = "",
                },
                Patches = [],
            },
            PreDownload = new GamePackageVersion { Patches = [] },
        };
    }
}
