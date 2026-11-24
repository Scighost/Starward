using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using Starward.Core.HoYoPlay;
using Starward.GameInstall;
using Starward.GameInstall.Model;
using Starward.GameInstall.Service;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Starward.Features.GameSelector;


public sealed partial class FindGamesDialog : ContentDialogBase
{

    private readonly ILogger<FindGamesDialog> _logger = AppConfig.GetLogger<FindGamesDialog>();

    private readonly HoYoPlayService _hoYoPlayService = AppConfig.GetService<HoYoPlayService>();


    public FindGamesDialog() : base()
    {
        InitializeComponent();
    }



    public List<FindGameModel> FindGames { get; set => SetProperty(ref field, value); }


    public bool GameInstallPathChanged { get; set; }




    [RelayCommand]
    private async Task FindFromRegistryAsync()
    {
        try
        {
            Grid_SelectFolder.Visibility = Visibility.Collapsed;
            StackPanel_Searching.Visibility = Visibility.Visible;

            List<LauncherConfigModel> launchers = LauncherConfigModel.GetEnabled();
            List<FindGameModel> models = [];

            foreach (LauncherConfigModel launcher in launchers)
            {
                string? registryKey = launcher.Id switch
                {
                    "jGHBHlcOq1" => @"Software\miHoYo\HYP\1_1",
                    "VYTpXlbWo8" => @"Software\Cognosphere\HYP\1_0",
                    _ => null,
                };
                if (registryKey is null)
                {
                    continue;
                }
                using RegistryKey? launcherKey = Registry.CurrentUser.OpenSubKey(registryKey);
                if (launcherKey is null)
                {
                    continue;
                }
                List<GameInfo> gameInfos = await _hoYoPlayService.GetGameInfosAsync(launcher.ToLauncherConfig(), UnloadCancellationToken);
                string[] gameBizs = launcherKey.GetSubKeyNames();
                foreach (string gameBiz in gameBizs)
                {
                    using var gameKey = launcherKey.OpenSubKey(gameBiz);
                    string? installPath = gameKey?.GetValue("GameInstallPath") as string;
                    if (Directory.Exists(installPath) && gameInfos.FirstOrDefault(x => x.GameBiz == gameBiz) is GameInfo gameInfo)
                    {
                        GameConfig? gameConfig = await _hoYoPlayService.GetGameConfigAsync(gameInfo, UnloadCancellationToken);
                        if (gameConfig is not null)
                        {
                            models.Add(new FindGameModel
                            {
                                IsSelected = true,
                                LauncherConfig = launcher,
                                GameInfo = gameInfo,
                                GameConfig = gameConfig,
                                Paths = [new FindGameModelPath
                                {
                                    InstallPath = installPath,
                                    Version = (await GameConfigService.GetGameConfigIniAsync(installPath, UnloadCancellationToken))?.GameVersion?.ToString(),
                                    SizeText = GetFolderSize(installPath),
                                }],
                            });
                        }
                    }
                }
            }

            if (models.Count is 0)
            {
                Grid_SelectFolder.Visibility = Visibility.Visible;
                TextBlock_NoGameResource.Visibility = Visibility.Visible;
                TextBlock_NoGameResource.Text = Lang.FindGamesDialog_NoGameFilesWereFound;
                return;
            }
            FindGames = models;
        }
        catch (OperationCanceledException)
        {
            Grid_SelectFolder.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Grid_SelectFolder.Visibility = Visibility.Visible;
            TextBlock_NoGameResource.Visibility = Visibility.Visible;
            TextBlock_NoGameResource.Text = ex.Message;
            _logger.LogError(ex, "Find game resource.");
        }
        finally
        {
            StackPanel_Searching.Visibility = Visibility.Collapsed;
        }
    }



    [RelayCommand]
    private async Task FindFolderAsync()
    {
        try
        {
            string? folder = await FileDialogHelper.PickFolderAsync(XamlRoot);
            if (Directory.Exists(folder))
            {
                await FindByFolderAsync([folder], UnloadCancellationToken);
            }
        }
        catch { }
    }



    [RelayCommand]
    private async Task FindAllDriveAsync()
    {
        try
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            IEnumerable<string> folders = drives.Where(x => x.DriveType is DriveType.Fixed && !DriveHelper.IsDeviceRemovableOrOnUSB(x.RootDirectory.FullName)).Select(x => x.RootDirectory.FullName);
            await FindByFolderAsync(folders, UnloadCancellationToken);
        }
        catch { }
    }



    private async Task FindByFolderAsync(IEnumerable<string> folders, CancellationToken cancellation)
    {
        try
        {
            Grid_SelectFolder.Visibility = Visibility.Collapsed;
            StackPanel_Searching.Visibility = Visibility.Visible;

            List<FindGameModel> models = await GetFindGameModelsAsync(cancellation);
            HashSet<string> exes = [];
            foreach (FindGameModel item in models)
            {
                exes.Add(item.GameConfig.ExeFileName);
            }

            List<FindGameModel> result = await GetResultAsync(folders, exes, models, cancellation);
            if (result.Count is 0)
            {
                Grid_SelectFolder.Visibility = Visibility.Visible;
                TextBlock_NoGameResource.Visibility = Visibility.Visible;
                TextBlock_NoGameResource.Text = Lang.FindGamesDialog_NoGameFilesWereFound;
                return;
            }
            result.ForEach(x => x.IsSelected = true);
            FindGames = result;
        }
        catch (OperationCanceledException)
        {
            Grid_SelectFolder.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            Grid_SelectFolder.Visibility = Visibility.Visible;
            TextBlock_NoGameResource.Visibility = Visibility.Visible;
            TextBlock_NoGameResource.Text = ex.Message;
            _logger.LogError(ex, "Find game resource.");
        }
        finally
        {
            StackPanel_Searching.Visibility = Visibility.Collapsed;
        }
    }



    private async Task<List<FindGameModel>> GetFindGameModelsAsync(CancellationToken cancellation)
    {
        List<LauncherConfigModel> launchers = LauncherConfigModel.GetEnabled();
        List<FindGameModel> models = [];

        foreach (LauncherConfigModel launcher in launchers)
        {
            var gameInfos = await _hoYoPlayService.GetGameInfosAsync(launcher.ToLauncherConfig(), cancellation);
            foreach (GameInfo gameInfo in gameInfos)
            {
                if (gameInfo.DisplayStatus is GameInfoDisplayStatus.LAUNCHER_GAME_DISPLAY_STATUS_AVAILABLE)
                {
                    if (gameInfo.GameServerConfigs?.Count > 0)
                    {
                        foreach (GameServerConfig server in gameInfo.GameServerConfigs)
                        {
                            GameId gameId = new GameId(server.GameId, gameInfo.GameBiz);
                            GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(gameId, cancellation);
                            if (config is not null)
                            {
                                FindGameModel model = new()
                                {
                                    LauncherConfig = launcher,
                                    GameInfo = gameInfo,
                                    GameConfig = config,
                                    ScanInfo = await _hoYoPlayService.GetGameScanInfoAsync(gameId, cancellation)
                                };
                                models.Add(model);
                            }
                        }
                    }
                    else
                    {
                        GameConfig? config = await _hoYoPlayService.GetGameConfigAsync(gameInfo, cancellation);
                        if (config is not null)
                        {
                            FindGameModel model = new()
                            {
                                LauncherConfig = launcher,
                                GameInfo = gameInfo,
                                GameConfig = config,
                                ScanInfo = await _hoYoPlayService.GetGameScanInfoAsync(gameInfo, cancellation)
                            };
                            models.Add(model);
                        }
                    }
                }
            }
        }

        return models;
    }


    private static List<FindGameModel> GetMatchesModel(List<FindGameModel> models, string name, string? md5)
    {
        List<FindGameModel> list = new();
        foreach (FindGameModel item in models)
        {
            if (string.Equals(item.GameConfig.ExeFileName, name, StringComparison.OrdinalIgnoreCase))
            {
                if (item.ScanInfo is null)
                {
                    list.Add(item);
                }
                else
                {
                    foreach (GameScanInfoExe scan in item.ScanInfo.GameExeList)
                    {
                        if (string.Equals(scan.MD5, md5, StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(item);
                            break;
                        }
                    }
                }
            }
        }
        return list;
    }


    private static List<FindGameModel> GetFallbackMatches(List<FindGameModel> models, string name, string gameBiz)
    {
        List<FindGameModel> list = new();
        foreach (FindGameModel item in models)
        {
            if (string.Equals(item.GameConfig.ExeFileName, name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.GameInfo.GameBiz.Value, gameBiz, StringComparison.OrdinalIgnoreCase))
            {
                list.Add(item);
            }
        }
        return list;
    }


    private static string? GetGameVersion(FindGameModel model, string? md5, GameConfigIni? configIni)
    {
        string? version = model.ScanInfo?.GameExeList?.FirstOrDefault(x => string.Equals(x.MD5, md5, StringComparison.OrdinalIgnoreCase))?.Version;
        if (string.IsNullOrWhiteSpace(version))
        {
            return configIni?.GameVersion?.ToString();
        }
        else
        {
            return version;
        }
    }



    private async Task<List<FindGameModel>> GetResultAsync(IEnumerable<string> folders, HashSet<string> exes, List<FindGameModel> models, CancellationToken cancellation)
    {
        EnumerationOptions option = new EnumerationOptions
        {
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint | FileAttributes.Offline | (FileAttributes)0x440000,
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
        };

        List<FindGameModel> result = new();

        await Task.Run(async () =>
        {
            foreach (string folder in folders)
            {
                foreach (string file in Directory.EnumerateFiles(folder, "*.exe", option))
                {
                    cancellation.ThrowIfCancellationRequested();
                    string name = Path.GetFileName(file);
                    if (exes.Contains(name))
                    {
                        string? md5 = await HashService.GetFileMD5Async(file, cancellation);
                        string dir = Path.GetDirectoryName(file)!;
                        GameConfigIni? configIni = await GameConfigService.GetGameConfigIniAsync(dir, cancellation);
                        var list = GetMatchesModel(models, name, md5);
                        if (list.Count == 0 && configIni is not null && !string.IsNullOrWhiteSpace(configIni.GameBiz))
                        {
                            list = GetFallbackMatches(models, name, configIni.GameBiz);
                        }
                        if (list.Count == 1)
                        {
                            FindGameModel item = list[0];
                            item.Paths.Add(new FindGameModelPath
                            {
                                InstallPath = dir,
                                Version = GetGameVersion(item, md5, configIni),
                                SizeText = GetFolderSize(dir),
                            });
                            if (!result.Contains(item))
                            {
                                result.Add(item);
                            }
                        }
                        else if (list.Count > 0)
                        {
                            if (configIni is not null && list.FirstOrDefault(x => x.LauncherConfig.Channel == configIni.Channel && x.LauncherConfig.SubChannel == configIni.SubChannel) is FindGameModel item)
                            {
                                item.Paths.Add(new FindGameModelPath
                                {
                                    InstallPath = dir,
                                    Version = GetGameVersion(item, md5, configIni),
                                    SizeText = GetFolderSize(dir),
                                });
                                if (!result.Contains(item))
                                {
                                    result.Add(item);
                                }
                            }
                            else
                            {
                                foreach (FindGameModel item1 in list)
                                {
                                    item1.Paths.Add(new FindGameModelPath
                                    {
                                        InstallPath = dir,
                                        Version = GetGameVersion(item1, md5, configIni),
                                        SizeText = GetFolderSize(dir),
                                    });
                                    if (!result.Contains(item1))
                                    {
                                        result.Add(item1);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }, cancellation);

        return result;
    }


    private static string GetFolderSize(string folder)
    {
        long size = new DirectoryInfo(folder).EnumerateFiles("*", SearchOption.AllDirectories).Sum(x => x.Length);
        return $"{(double)size / (1 << 30):F2} GB";
    }



    [RelayCommand]
    private async Task AddGames()
    {
        try
        {
            if (FindGames is null)
            {
                return;
            }
            if (FindGames.Count(x => x.IsSelected) is 0)
            {
                return;
            }

            foreach (FindGameModel? item in FindGames.Where(x => x.IsSelected))
            {
                string path = item.Paths[item.SelectIndex].InstallPath;
                GameEntryService.SetInstallPath(item.GameInfo, item.LauncherConfig.Id, path, false);
                WeakReferenceMessenger.Default.Send(new PinGameIconMessage(item.GameInfo, item.LauncherConfig, true));
            }
            GameInstallPathChanged = true;
            Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Find games: add games.");
        }
    }
}



public class FindGameModel : ObservableObject
{
    public LauncherConfigModel LauncherConfig { get; set; }

    public GameInfo GameInfo { get; set; }

    public GameConfig GameConfig { get; set; }

    public GameScanInfo? ScanInfo { get; set; }

    public bool IsSelected { get; set; }

    public int SelectIndex { get; set; }

    public List<FindGameModelPath> Paths { get; set; } = new();
}


public class FindGameModelPath
{
    public string InstallPath { get; set; }

    public string? Version { get; set; }

    public string SizeText { get; set; }
}