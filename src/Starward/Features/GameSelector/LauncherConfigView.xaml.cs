using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Starward.Core.HoYoPlay;
using Starward.Features.GameLauncher;
using Starward.Features.Setting;
using Starward.Features.ViewHost;
using Starward.GameInstall;
using Starward.GameInstall.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.System;


namespace Starward.Features.GameSelector;

[INotifyPropertyChanged]
public sealed partial class LauncherConfigView : UserControl
{

    private readonly ILogger<LauncherConfigView> _logger = AppConfig.GetLogger<LauncherConfigView>();

    private readonly HoYoPlayService _hoYoPlayService = AppConfig.GetService<HoYoPlayService>();



    public LauncherConfigView()
    {
        InitializeComponent();
        this.Loaded += LauncherConfigView_Loaded;
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, HandleLanguageChangedMessage);
        WeakReferenceMessenger.Default.Register<GameInstallPathChangedMessage>(this, HandleGameInstallPathChangedMessage);
    }


    public GameId? CurrentGameId { get; set; }

    public ObservableCollection<LauncherConfigModel> LauncherConfigs { get; set => SetProperty(ref field, value); }

    public ObservableCollection<GameInfoModel>? GameInfos { get; set => SetProperty(ref field, value); }

    private LauncherConfigModel _selectedLauncherConfig;

    public GameInfo? SelectedGameInfo { get; set => SetProperty(ref field, value); }

    public GameReservationContent? ReservationContent { get; set => SetProperty(ref field, value); }

    public bool ReservationViewLoad { get; set => SetProperty(ref field, value); }


    private async void LauncherConfigView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadContentAsync();
    }


    private async Task LoadContentAsync()
    {
        try
        {
            LauncherConfigs = new(LauncherConfigModel.GetEnabled());
            await UpdateInstalledGamesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load content");
        }
    }


    [RelayCommand]
    public void GoBack()
    {
        if (ReservationViewLoad && ReservationView.IsHitTestVisible)
        {
            ReservationContent = null;
            ReservationView.SelectedMedia = null;
            ReservationView.IsHitTestVisible = false;
            ReservationView.Opacity = 0;
        }
        else
        {
            this.Opacity = 0;
            this.IsHitTestVisible = false;
            ReservationViewLoad = false;
            CloseGameInfoGrid();
            TeachingTip_FindGames.IsOpen = false;
        }
    }



    public async void ShowGameInfosIfNoGames()
    {
        try
        {
            if (CurrentGameId is null || InstalledGames?.Count is 0)
            {
                if (_selectedLauncherConfig is not null)
                {
                    int index = LauncherConfigs?.IndexOf(_selectedLauncherConfig) ?? -1;
                    if (index > 0)
                    {
                        ListView_LauncherConfig.SelectedIndex = index;
                        return;
                    }
                }
                ListView_LauncherConfig.SelectedIndex = 0;
            }
            if (!AppConfig.ShownFindGamesTip)
            {
                await Task.Delay(1000);
                TeachingTip_FindGames.IsOpen = true;
            }
        }
        catch { }
    }


    private void TeachingTip_FindGames_CloseButtonClick(TeachingTip sender, object args)
    {
        AppConfig.ShownFindGamesTip = true;
        TeachingTip_FindGames.IsOpen = false;
    }


    private void HandleLanguageChangedMessage(object sender, LanguageChangedMessage m)
    {
        try
        {
            _ = UpdateInstalledGamesAsync();
        }
        catch { }
    }



    [RelayCommand]
    private async Task OpenLauncherConfigSettingDialogAsync()
    {
        try
        {
            var dialog = new LauncherConfigSettingDialog
            {
                XamlRoot = this.XamlRoot,
            };
            await dialog.ShowAsync();
            if (dialog.EnabledLauncherChanged)
            {
                CloseGameInfoGrid();
                await LoadContentAsync();
                WeakReferenceMessenger.Default.Send(new EnabledLauncherConfigChangedMessage());
                await _hoYoPlayService.UpdateLauncherClientMapAsync(LauncherConfigs.Select(x => x.ToLauncherConfig()));
            }
        }
        catch { }
    }



    #region Installed Games


    public ObservableCollection<InstalledGameModel> InstalledGames { get; set => SetProperty(ref field, value); }


    public string? ActualStorageSizeText { get; set => SetProperty(ref field, value); }


    [RelayCommand]
    private async Task FindGamesAsync()
    {
        try
        {
            FindGamesDialog dialog = new()
            {
                XamlRoot = this.XamlRoot,
            };
            await dialog.ShowAsync();
            if (dialog.GameInstallPathChanged)
            {
                await UpdateInstalledGamesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Find games.");
        }
    }


    private CancellationTokenSource? _updateInstalledGamesTokenSource;

    [RelayCommand]
    private async Task UpdateInstalledGamesAsync()
    {
        _updateInstalledGamesTokenSource?.Cancel();
        _updateInstalledGamesTokenSource = new();
        CancellationToken cancellation = _updateInstalledGamesTokenSource.Token;
        try
        {
            ActualStorageSizeText = null;
            List<GameEntry> entries = GameEntryService.GetInstalledEntries();
            List<InstalledGameModel> models = await GetInstalledGameModelsAsync(cancellation);
            cancellation.ThrowIfCancellationRequested();
            InstalledGames = new(models);
            await Parallel.ForEachAsync(models, cancellation, UpdateGameResourceDatabaseAsync);
            await UpdateHardLinkSharedSizeAsync(models, cancellation);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update installed games.");
        }
    }


    private async Task<List<InstalledGameModel>> GetInstalledGameModelsAsync(CancellationToken cancellation)
    {
        const double GB = 1 << 30;
        List<GameEntry> entries = GameEntryService.GetInstalledEntries();
        List<InstalledGameModel> models = new();
        await Task.Run(async () =>
        {
            foreach (var item in entries)
            {
                cancellation.ThrowIfCancellationRequested();
                if (Directory.Exists(item.InstallPath))
                {
                    GameId gameId = new(item.GameId, item.GameBiz);
                    LauncherConfigModel? launcher = LauncherConfigs.FirstOrDefault(x => x.Id == item.LauncherId);
                    if (launcher is null)
                    {
                        continue;
                    }
                    GameInfo? gameInfo = await _hoYoPlayService.GetGameInfoAsync(gameId, cancellation);
                    long size = new DirectoryInfo(item.InstallPath).EnumerateFiles("*", SearchOption.AllDirectories).Sum(x => x.Length);
                    InstalledGameModel model = new()
                    {
                        GameId = gameId,
                        LauncherId = item.LauncherId,
                        InstallPath = item.InstallPath,
                        Removable = item.Removable,
                        IsPinned = item.IsPinned,
                        GameName = gameInfo?.Display.Name!,
                        GameIcon = PinnedGameIcon.GetGameIcon(gameId.GameBiz.Game) ?? gameInfo?.Display.Icon.Url!,
                        LauncherName = launcher.Name,
                        LauncherIcon = launcher.SmallIcon,
                        SizeText = $"{size / GB:F2} GB",
                    };
                    if (gameInfo?.GameServerConfigs?.FirstOrDefault(x => x.GameId == item.GameId) is GameServerConfig serverConfig)
                    {
                        model.GameName = serverConfig.I18nName;
                    }
                    models.Add(model);
                }
            }
        }, cancellation);
        return models;
    }


    private async ValueTask UpdateGameResourceDatabaseAsync(InstalledGameModel model, CancellationToken cancellation)
    {
        await Task.Run(() =>
        {
            using var dapper = GameFileDatabase.CreateConnection();
            var oldDict = dapper.Query<GameFileItem>("SELECT * FROM GameFile WHERE GameId = @Id;", new { model.GameId.Id }).ToDictionary(x => x.Path);

            List<GameFileItem> upserts = new();
            if (new DriveInfo(model.InstallPath).DriveFormat is "NTFS" or "ReFS")
            {
                ulong volumeSerialNumber;
                using (Kernel32.SafeHFILE volumeHandle = Kernel32.CreateFile(
                    model.InstallPath,
                    Kernel32.FileAccess.GENERIC_READ,
                    FileShare.ReadWrite | FileShare.Delete,
                    null,
                    FileMode.Open,
                    FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS,
                    HFILE.NULL))
                {
                    var volumeInfo = Kernel32.GetFileInformationByHandleEx<Kernel32.FILE_ID_INFO>(volumeHandle, Kernel32.FILE_INFO_BY_HANDLE_CLASS.FileIdInfo);
                    volumeSerialNumber = volumeInfo.VolumeSerialNumber;
                }
                var file = GetFilesFromFolder(model.InstallPath, cancellation);
                foreach ((string Path, long LastWriteTime, long Size, Kernel32.FILE_ID_128 FileId) in file)
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (oldDict.Remove(Path, out GameFileItem? oldItem))
                    {
                        if (LastWriteTime != oldItem.LastWriteTime)
                        {
                            oldItem.LastWriteTime = LastWriteTime;
                            oldItem.Size = Size;
                            oldItem.MD5 = null;
                            oldItem.VolumeSerialNumber = volumeSerialNumber;
                            oldItem.FileId = FileId;
                            upserts.Add(oldItem);
                        }
                    }
                    else
                    {
                        var newItem = new GameFileItem
                        {
                            Path = Path,
                            GameId = model.GameId.Id,
                            Size = Size,
                            LastWriteTime = LastWriteTime,
                            VolumeSerialNumber = volumeSerialNumber,
                            FileId = FileId,
                        };
                        upserts.Add(newItem);
                    }
                }
            }
            else
            {
                foreach (FileInfo fileInfo in new DirectoryInfo(model.InstallPath).EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (oldDict.Remove(fileInfo.FullName, out GameFileItem? oldItem))
                    {
                        long time = fileInfo.LastWriteTime.ToFileTimeUtc();
                        if (time != oldItem.LastWriteTime)
                        {
                            oldItem.LastWriteTime = time;
                            oldItem.Size = fileInfo.Length;
                            oldItem.MD5 = null;
                            upserts.Add(oldItem);
                        }
                    }
                    else
                    {
                        var newItem = new GameFileItem
                        {
                            Path = fileInfo.FullName,
                            GameId = model.GameId.Id,
                            Size = fileInfo.Length,
                            LastWriteTime = fileInfo.LastWriteTime.ToFileTimeUtc(),
                        };
                        upserts.Add(newItem);
                    }
                }
            }

            cancellation.ThrowIfCancellationRequested();
            using var t = dapper.BeginTransaction();

            if (oldDict.Count > 0)
            {
                foreach (GameFileItem? item in oldDict.Values)
                {
                    dapper.Execute("DELETE FROM GameFile WHERE Path = @Path;", item, t);
                }
            }
            if (upserts.Count > 0)
            {
                dapper.Execute("""
                    INSERT INTO GameFile (Path, GameId, VolumeId, FileIdLow, FileIdHigh, Size, MD5, LastWriteTime)
                    VALUES (@Path, @GameId, @VolumeId, @FileIdLow, @FileIdHigh, @Size, @MD5, @LastWriteTime)
                    ON CONFLICT (Path) DO UPDATE SET GameId        = excluded.GameId,
                                                     VolumeId      = excluded.VolumeId,
                                                     FileIdLow     = excluded.FileIdLow,
                                                     FileIdHigh    = excluded.FileIdHigh,
                                                     Size          = excluded.Size,
                                                     MD5           = excluded.MD5,
                                                     LastWriteTime = excluded.LastWriteTime;
                    """, upserts, t);
            }
            t.Commit();
        }, cancellation);
    }


    private async Task UpdateHardLinkSharedSizeAsync(List<InstalledGameModel> models, CancellationToken cancellation)
    {
        const double GB = 1 << 30;
        const double MB = 1 << 20;
        string[] allIds = models.Select(x => x.GameId.Id).ToArray();
        using var dapper = GameFileDatabase.CreateConnection();
        foreach (InstalledGameModel model in models)
        {
            cancellation.ThrowIfCancellationRequested();
            long sharedSize = await Task.Run(() => dapper.QuerySingleOrDefault<long>("""
                SELECT COALESCE(SUM(g.Size), 0) AS HardLinkSize FROM GameFile AS g
                WHERE g.GameId = @GameId AND g.VolumeId <> 0 AND (g.FileIdLow <> 0 OR g.FileIdHigh <> 0)
                AND EXISTS (SELECT 1 FROM GameFile AS o WHERE o.VolumeId = g.VolumeId AND o.FileIdLow = g.FileIdLow AND o.FileIdHigh = g.FileIdHigh AND o.Path <> g.Path);
                """, new { GameId = model.GameId.Id }), cancellation);
            if (sharedSize >= (1 << 30))
            {
                model.SharedSizeText = $"{sharedSize / GB:F2} GB";
                var res = await Task.Run(() => dapper.Query<(string GameId, long HardLinkSize)>("""
                    WITH Partners AS (
                        SELECT DISTINCT g.Path, g.Size, o.GameId FROM GameFile AS g JOIN GameFile AS o
                        ON o.VolumeId = g.VolumeId AND o.FileIdLow = g.FileIdLow AND o.FileIdHigh = g.FileIdHigh AND o.Path <> g.Path
                        WHERE g.GameId = @GameId AND g.VolumeId <> 0 AND (g.FileIdLow <> 0 OR g.FileIdHigh <> 0) AND o.GameId <> @GameId AND o.GameId IN @AllGameIds
                    )
                    SELECT GameId, SUM(Size) AS HardLinkSize FROM Partners GROUP BY GameId ORDER BY HardLinkSize DESC;
                    """, new { GameId = model.GameId.Id, AllGameIds = allIds }), cancellation);

                var sharedModels = new List<InstalledGameHardLinkSharedModel>();
                foreach ((string gameId, long size) in res)
                {
                    if (size >= (1 << 30))
                    {
                        if (models.FirstOrDefault(x => x.GameId.Id == gameId) is InstalledGameModel link)
                        {
                            sharedModels.Add(new()
                            {
                                GameIcon = link.GameIcon,
                                GameName = link.GameName,
                                LauncherIcon = link.LauncherIcon,
                                LauncherName = link.LauncherName,
                                SizeText = size switch
                                {
                                    >= (1 << 30) => $"{size / GB:F2} GB",
                                    _ => $"{size / MB:F2} MB",
                                }
                            });
                        }
                    }
                }
                model.SharedModels = sharedModels;
            }
        }
        long totalSize = await Task.Run(() => dapper.QuerySingleOrDefault<long>("""
            WITH Deduped AS (SELECT MIN(Size) AS Size FROM GameFile WHERE VolumeId <> 0 AND (FileIdLow <> 0 OR FileIdHigh <> 0) AND GameId IN @AllGameIds GROUP BY VolumeId, FileIdLow, FileIdHigh
                UNION ALL SELECT Size FROM GameFile WHERE (VolumeId = 0 OR (FileIdLow = 0 AND FileIdHigh = 0)) AND GameId IN @AllGameIds)
            SELECT COALESCE(SUM(Size), 0) AS OccupiedSize FROM Deduped;
            """, new { AllGameIds = allIds }), cancellation);
        ActualStorageSizeText = $"{totalSize / GB:F2} GB";
    }


    private List<(string Path, long LastWriteTime, long Size, Kernel32.FILE_ID_128 FileId)> GetFilesFromFolder(string path, CancellationToken cancellation)
    {
        List<(string Path, long LastWriteTime, long Size, Kernel32.FILE_ID_128 FileId)> files = new();
        List<string> folders = [path, .. Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)];

        const int bufferSize = 64 * 1024;
        nint buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            int fileNameOffset = Marshal.OffsetOf<Kernel32.FILE_ID_EXTD_DIR_INFO>(nameof(Kernel32.FILE_ID_EXTD_DIR_INFO.FileName)).ToInt32();
            foreach (string folder in folders)
            {
                cancellation.ThrowIfCancellationRequested();
                using Kernel32.SafeHFILE handle = Kernel32.CreateFile(
                    folder,
                    Kernel32.FileAccess.GENERIC_READ,
                    FileShare.ReadWrite | FileShare.Delete,
                    null,
                    FileMode.Open,
                    FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS,
                    HFILE.NULL);
                if (handle.IsInvalid)
                {
                    continue;
                }

                while (true)
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (!Kernel32.GetFileInformationByHandleEx(handle, Kernel32.FILE_INFO_BY_HANDLE_CLASS.FileIdExtdDirectoryInfo, buffer, bufferSize))
                    {
                        break;
                    }

                    for (int offset = 0; ;)
                    {
                        Kernel32.FILE_ID_EXTD_DIR_INFO info = Marshal.PtrToStructure<Kernel32.FILE_ID_EXTD_DIR_INFO>(buffer + offset);
                        if ((info.FileAttributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY) == 0)
                        {
                            string name = Marshal.PtrToStringUni(buffer + offset + fileNameOffset, (int)(info.FileNameLength / 2)) ?? "";
                            if (name.Length > 0)
                            {
                                long lastWriteTime = ((long)(uint)info.LastWriteTime.dwHighDateTime << 32) | (uint)info.LastWriteTime.dwLowDateTime;
                                files.Add((System.IO.Path.Combine(folder, name), lastWriteTime, info.EndOfFile, info.FileId));
                            }
                        }
                        if (info.NextEntryOffset == 0)
                        {
                            break;
                        }
                        offset += (int)info.NextEntryOffset;
                    }
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
        return files;
    }


    private void HandleGameInstallPathChangedMessage(object sender, GameInstallPathChangedMessage m)
    {
        try
        {
            this.Bindings.Update();
            _ = UpdateInstalledGamesAsync();
        }
        catch { }
    }


    private async void Button_OpenGameFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement { DataContext: InstalledGameModel model })
            {
                if (Directory.Exists(model.InstallPath))
                {
                    await Launcher.LaunchUriAsync(new(model.InstallPath));
                }
            }
        }
        catch { }
    }


    #endregion



    #region  Game Info and Display


    private CancellationTokenSource? _getGameInfoTokenSource;

    private async void ListView_LauncherConfig_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _getGameInfoTokenSource?.Cancel();
        _getGameInfoTokenSource = new();
        CancellationToken token = _getGameInfoTokenSource.Token;
        try
        {
            if (e.AddedItems.FirstOrDefault() is LauncherConfigModel model)
            {
                var list = await _hoYoPlayService.GetGameInfosAsync(model.ToLauncherConfig(), token);
                var pinnedEntries = GameEntryService.GetPinnedEntries().Select(x => x.GameId).ToHashSet();
                GameInfos = new(list.Select(x => new GameInfoModel(x) { IsPinned = pinnedEntries.Contains(x.Id) }));
                _selectedLauncherConfig = model;
                if (GameInfos.Count > 0)
                {
                    SelectedGameInfo = GameInfos.First().GameInfo;
                    ShowGameIntroduction();
                    await Task.Delay(100, token);
                    _lastGameDisplayGrid = (GridView_GameInfos.ContainerFromIndex(0) as GridViewItem)?.ContentTemplateRoot as Grid;
                    (_lastGameDisplayGrid?.Resources["ProgressStoryboard"] as Storyboard)?.Begin();
                    (_lastGameDisplayGrid?.FindName("Rectangle_BorderAnimation") as Rectangle)?.Opacity = 1;
                    Grid_InstalledGames.Visibility = Visibility.Collapsed;
                }
                if (GameInfos.Count > 8)
                {
                    Button_ScrollLeft.Visibility = Visibility.Visible;
                    Button_ScrollRight.Visibility = Visibility.Visible;
                }
                else
                {
                    Button_ScrollLeft.Visibility = Visibility.Collapsed;
                    Button_ScrollRight.Visibility = Visibility.Collapsed;
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Launcher config changed.");
        }
    }


    private Grid? _lastGameDisplayGrid;


    private void Grid_GameDisplay_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            (grid.FindName("CachedImage_Logo") as UIElement)?.Opacity = 0;
            (grid.FindName("Border_Mask") as UIElement)?.Opacity = 1;
            (grid.FindName("StackPanel_ViewDetails") as UIElement)?.Opacity = 1;
            (grid.FindName("StackPanel_ViewDetails") as UIElement)?.Translation = Vector3.Zero;

            if (grid.DataContext is GameInfoModel model && grid != _lastGameDisplayGrid)
            {
                SelectedGameInfo = model.GameInfo;
                ShowGameIntroduction();
                (grid.Resources["ProgressStoryboard"] as Storyboard)?.Begin();
                (grid?.FindName("Rectangle_BorderAnimation") as Rectangle)?.Opacity = 1;
                (_lastGameDisplayGrid?.FindName("Rectangle_BorderAnimation") as Rectangle)?.Opacity = 0;
                _lastGameDisplayGrid = grid;
            }
        }
    }


    private void Grid_GameDisplay_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            (grid.FindName("CachedImage_Logo") as UIElement)?.Opacity = 1;
            (grid.FindName("Border_Mask") as UIElement)?.Opacity = 0;
            (grid.FindName("StackPanel_ViewDetails") as UIElement)?.Opacity = 0;
            (grid.FindName("StackPanel_ViewDetails") as UIElement)?.Translation = new Vector3(0, 12, 0);
        }
    }


    [RelayCommand]
    private void CloseGameInfoGrid()
    {
        GameInfos = null;
        ListView_LauncherConfig.SelectedItem = null;
        Grid_Background.Opacity = 0;
        Grid_InstalledGames.Visibility = Visibility.Visible;
    }


    private CancellationTokenSource? _showIntroTokenSource;


    private async void ShowGameIntroduction()
    {
        try
        {
            _showIntroTokenSource?.Cancel();
            _showIntroTokenSource = new();
            Grid_Background.Opacity = 1;
            CachedImage_TopLeftLogo.Opacity = 0;
            TextBlock_Introduction.Opacity = 0;
            CachedImage_TopLeftLogo.Visibility = Visibility.Collapsed;
            TextBlock_Introduction.Visibility = Visibility.Collapsed;
            CachedImage_TopLeftLogo.Translation = new Vector3(0, 20, 0);
            TextBlock_Introduction.Translation = new Vector3(0, 20, 0);
            await Task.Delay(300, _showIntroTokenSource.Token);
            CachedImage_TopLeftLogo.Opacity = 1;
            TextBlock_Introduction.Opacity = 1;
            CachedImage_TopLeftLogo.Visibility = Visibility.Visible;
            TextBlock_Introduction.Visibility = Visibility.Visible;
            CachedImage_TopLeftLogo.Translation = Vector3.Zero;
            TextBlock_Introduction.Translation = Vector3.Zero;
        }
        catch { }

    }


    private void Button_Pin_Tapped(object sender, TappedRoutedEventArgs e)
    {
        try
        {
            if (sender is Button { DataContext: GameInfoModel model })
            {
                model.IsPinned = !model.IsPinned;
                WeakReferenceMessenger.Default.Send(new PinGameIconMessage(model.GameInfo, _selectedLauncherConfig, model.IsPinned));
            }
            e.Handled = true;
        }
        catch { }
    }


    private async void Grid_GameDisplay_Tapped(object sender, TappedRoutedEventArgs e)
    {
        try
        {
            if (sender is Grid { DataContext: GameInfoModel model })
            {
                if (model.GameInfo.DisplayStatus is GameInfoDisplayStatus.LAUNCHER_GAME_DISPLAY_STATUS_RESERVATION_ENABLED)
                {
                    var content = await _hoYoPlayService.GetGameReservationContentAsync(model.GameInfo);
                    if (content?.Medias?.Count > 0)
                    {
                        ReservationContent = content;
                        ReservationViewLoad = true;
                        ReservationView.HomePageUrl = model.GameInfo.Reservation?.Link;
                        ReservationView.IsHitTestVisible = true;
                        ReservationView.Opacity = 1;
                        await Task.Delay(100);
                        ReservationView.SelectedMedia = ReservationContent?.Medias.FirstOrDefault();
                    }
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(new MainViewNavigateMessage(typeof(GameLauncherPage), model.GameInfo));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GameDisplay tapped.");
        }
    }


    [RelayCommand]
    private void ScrollLeft()
    {
        try
        {
            var viewer = GridView_GameInfos.FindDescendant<ScrollViewer>();
            viewer?.ChangeView(viewer?.HorizontalOffset - 132, null, null);
        }
        catch { }
    }


    [RelayCommand]
    private void ScrollRight()
    {
        try
        {
            var viewer = GridView_GameInfos.FindDescendant<ScrollViewer>();
            viewer?.ChangeView(viewer?.HorizontalOffset + 132, null, null);
        }
        catch { }
    }







    #endregion


}
