using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.GameLauncher;
using Starward.Features.HoYoPlay;
using Starward.Features.Setting;
using Starward.Features.ViewHost;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Foundation;


namespace Starward.Features.GameSelector;

[INotifyPropertyChanged]
public sealed partial class GameSelector : UserControl
{


    public event EventHandler<(GameId, bool DoubleTapped)>? CurrentGameChanged;


    private readonly HoYoPlayService _hoyoplayService = AppService.GetService<HoYoPlayService>();


    private readonly GameLauncherService _gameLauncherService = AppService.GetService<GameLauncherService>();



    public GameSelector()
    {
        this.InitializeComponent();
        InitializeGameSelector();
        this.Loaded += GameSelector_Loaded;
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, OnLanguageChanged);
        WeakReferenceMessenger.Default.Register<MainWindowStateChangedMessage>(this, OnMainWindowStateChanged);
    }



    public GameBiz CurrentGameBiz { get; set; }


    public GameId? CurrentGameId { get; set; }


    public ObservableCollection<GameBizIcon> GameBizIcons { get; set => SetProperty(ref field, value); } = new();


    public GameBizIcon? CurrentGameBizIcon { get; set => SetProperty(ref field, value); }


    public bool IsPinned { get; set => SetProperty(ref field, value); }




    public void InitializeGameSelector()
    {
        List<GameInfo> gameInfos = GetCachedGameInfos();
        InitializeGameIconsArea(gameInfos);
        InitializeGameServerArea(gameInfos);
        InitializeInstalledGamesCommand.Execute(null);
    }



    private async void GameSelector_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(1000);
        await UpdateGameInfoAsync();
    }





    private async void OnLanguageChanged(object? _, LanguageChangedMessage __)
    {
        this.Bindings.Update();
        await UpdateGameInfoAsync();
    }




    private async void OnMainWindowStateChanged(object? _, MainWindowStateChangedMessage message)
    {
        try
        {
            if (message.Activate && (message.ElapsedOver(TimeSpan.FromMinutes(10)) || message.IsCrossingHour))
            {
                await UpdateGameInfoAsync();
            }
        }
        catch { }
    }




    private List<GameInfo> GetCachedGameInfos()
    {
        try
        {
            string? json = AppSetting.CachedGameInfo;
            if (!string.IsNullOrWhiteSpace(json))
            {
                return JsonSerializer.Deserialize<List<GameInfo>>(json) ?? [];
            }
        }
        catch { }
        return [];
    }




    #region Game Icon


    /// <summary>
    /// 初始化游戏图标区域
    /// </summary>
    private void InitializeGameIconsArea(List<GameInfo> gameInfos)
    {
        try
        {
            GameBizIcons.CollectionChanged -= GameBizIcons_CollectionChanged;
            GameBizIcons.Clear();

            // 从配置文件读取已选的 GameBiz
            string? bizs = AppSetting.SelectedGameBizs;
            foreach (string str in bizs?.Split(',') ?? [])
            {
                if (GameBiz.TryParse(str, out GameBiz biz))
                {
                    // 已知的 GameBiz
                    GameBizIcons.Add(new GameBizIcon(biz));
                }
                else if (gameInfos.FirstOrDefault(x => x.GameBiz == biz) is GameInfo info)
                {
                    // 由 HoYoPlay API 获取，但未适配的 GameBiz
                    GameBizIcons.Add(new GameBizIcon(info));
                }
            }

            // 选取当前游戏
            GameBiz lastSelectedGameBiz = AppSetting.CurrentGameBiz;
            if (GameBizIcons.FirstOrDefault(x => x.GameBiz == lastSelectedGameBiz) is GameBizIcon icon)
            {
                CurrentGameBizIcon = icon;
                CurrentGameBizIcon.IsSelected = true;
                CurrentGameBiz = lastSelectedGameBiz;
            }
            else if (lastSelectedGameBiz.IsKnown())
            {
                CurrentGameBizIcon = new GameBizIcon(lastSelectedGameBiz);
                CurrentGameBizIcon.IsSelected = true;
                CurrentGameBiz = lastSelectedGameBiz;
            }
            else if (gameInfos.FirstOrDefault(x => x.GameBiz == lastSelectedGameBiz) is GameInfo info)
            {
                CurrentGameBizIcon = new GameBizIcon(info);
                CurrentGameBizIcon.IsSelected = true;
                CurrentGameBiz = lastSelectedGameBiz;
            }

            CurrentGameId = CurrentGameBizIcon?.GameId;
            if (CurrentGameId is not null)
            {
                CurrentGameChanged?.Invoke(this, (CurrentGameId, false));
            }

            if (AppSetting.IsGameBizSelectorPinned)
            {
                Pin();
            }

            GameBizIcons.CollectionChanged += GameBizIcons_CollectionChanged;
        }
        catch { }
    }



    /// <summary>
    /// 游戏图标区域是否可见
    /// </summary>
    public bool GameIconsAreaVisible
    {
        get => Grid_GameIconsArea.Translation == Vector3.Zero;
        set
        {
            if (value)
            {
                Grid_GameIconsArea.Translation = Vector3.Zero;
            }
            else
            {
                Grid_GameIconsArea.Translation = new Vector3(0, -100, 0);
            }
            UpdateDragRectangles();
        }
    }


    /// <summary>
    /// 更新窗口拖拽区域
    /// </summary>
    public void UpdateDragRectangles()
    {
        try
        {
            double x = Border_CurrentGameIcon.ActualWidth;
            if (GameIconsAreaVisible)
            {
                x = Border_CurrentGameIcon.ActualWidth + Grid_GameIconsArea.ActualWidth;
            }
            this.XamlRoot.SetWindowDragRectangles([new Rect(x, 0, 10000, 48)]);
        }
        catch { }
    }



    /// <summary>
    /// 鼠标移入到当前游戏图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Border_CurrentGameIcon_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        // 显示所有游戏图标
        GameIconsAreaVisible = true;
    }



    /// <summary>
    /// 鼠标移出当前游戏图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Border_CurrentGameIcon_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (FullBackgroundVisible || IsPinned)
        {
            // 当前游戏图标被固定或者全屏显示时，不隐藏所有游戏图标
            return;
        }
        if (sender is UIElement ele)
        {
            var postion = e.GetCurrentPoint(sender as UIElement).Position;
            if (postion.X > ele.ActualSize.X - 1 && postion.Y > 0 && postion.Y < ele.ActualSize.Y)
            {
                // 从右侧移出，此时进入到所有游戏图标区域，不隐藏
                return;
            }
        }
        // 其他方向移出，隐藏所有游戏图标
        GameIconsAreaVisible = false;
    }



    /// <summary>
    /// 鼠标移出所有游戏图标区域
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Grid_GameIconsArea_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (FullBackgroundVisible || IsPinned)
        {
            return;
        }
        GameIconsAreaVisible = false;
    }



    /// <summary>
    /// 点击没有被选择的游戏图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Grid_GameIcon_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is GameBizIcon icon)
        {
            if (CurrentGameBizIcon is not null)
            {
                CurrentGameBizIcon.IsSelected = false;
            }

            CurrentGameBizIcon = icon;
            CurrentGameBiz = icon.GameBiz;
            CurrentGameId = icon.GameId;
            icon.IsSelected = true;

            CurrentGameChanged?.Invoke(this, (icon.GameId, false));
            AppSetting.CurrentGameBiz = icon.GameBiz;
        }
    }



    /// <summary>
    /// 双击没有被选择的游戏图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Grid_GameIcon_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is GameBizIcon icon)
        {
            if (CurrentGameBizIcon is not null)
            {
                CurrentGameBizIcon.IsSelected = false;
            }

            CurrentGameBizIcon = icon;
            CurrentGameBiz = icon.GameBiz;
            CurrentGameId = icon.GameId;
            icon.IsSelected = true;
            HideFullBackground();

            CurrentGameChanged?.Invoke(this, (icon.GameId, true));
            AppSetting.CurrentGameBiz = icon.GameBiz;
        }
    }


    /// <summary>
    /// 鼠标移入到游戏图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Grid_GameIcon_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is GameBizIcon icon)
        {
            if (!icon.IsSelected)
            {
                icon.MaskOpacity = 0;
            }
        }
    }



    /// <summary>
    /// 鼠标移出游戏图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Grid_GameIcon_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is GameBizIcon icon)
        {
            if (!icon.IsSelected)
            {
                icon.MaskOpacity = 1;
            }
        }
    }



    /// <summary>
    /// 游戏图标区域大小变化
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Grid_GameIconsArea_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDragRectangles();
    }



    /// <summary>
    /// 固定游戏待选区
    /// </summary>
    [RelayCommand]
    private void Pin()
    {
        IsPinned = !IsPinned;
        if (IsPinned)
        {
            GameIconsAreaVisible = true;
        }
        else
        {
            // 避免在固定时更换当前游戏，取消固定后，左上角的图标不改变的问题
            var temp = CurrentGameBizIcon;
            CurrentGameBizIcon = null;
            CurrentGameBizIcon = temp;
            if (!FullBackgroundVisible)
            {
                GameIconsAreaVisible = false;
            }
        }
        AppSetting.IsGameBizSelectorPinned = IsPinned;
    }



    /// <summary>
    /// 待选游戏变更时，保存配置
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void GameBizIcons_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        try
        {
            var sb = new StringBuilder();
            foreach (var icon in GameBizIcons)
            {
                sb.Append(icon.GameBiz);
                sb.Append(',');
            }
            AppSetting.SelectedGameBizs = sb.ToString().TrimEnd(',');
        }
        catch { }
    }



    #endregion





    #region Full Background 黑色半透明背景


    public bool FullBackgroundVisible => Border_FullBackground.Opacity > 0;


    [RelayCommand]
    private void ShowFullBackground()
    {
        Border_FullBackground.Opacity = 1;
        Border_FullBackground.IsHitTestVisible = true;
        GameIconsAreaVisible = true;
    }


    private void HideFullBackground()
    {
        Border_FullBackground.Opacity = 0;
        Border_FullBackground.IsHitTestVisible = false;
        if (!IsPinned)
        {
            GameIconsAreaVisible = false;
        }
    }


    private void Border_FullBackground_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        _isGameBizDisplayPressed = false;
        var position = e.GetPosition(sender as UIElement);
        if (position.X <= Border_CurrentGameIcon.ActualWidth && position.Y <= Border_CurrentGameIcon.ActualHeight)
        {
            Border_FullBackground.Opacity = 0;
            Border_FullBackground.IsHitTestVisible = false;
        }
        else
        {
            HideFullBackground();
        }
    }


    #endregion





    #region Game Server



    public List<GameBizDisplay> GameBizDisplays { get; set => SetProperty(ref field, value); }




    /// <summary>
    /// 初始化游戏服务器选择区域
    /// </summary>
    private void InitializeGameServerArea(List<GameInfo> gameInfos)
    {
        try
        {
            var list = new List<GameBizDisplay>();

            if (LanguageUtil.FilterLanguage(CultureInfo.CurrentUICulture.Name) is "zh-cn")
            {
                // 当前语言为简体中文时，游戏信息显示从中国官服获取的内容
                foreach (var info in gameInfos)
                {
                    if (info.GameBiz.IsChinaServer() && !info.IsBilibiliServer())
                    {
                        list.Add(new GameBizDisplay { GameInfo = info });
                    }
                }
            }
            else
            {
                // 当前语言不为简体中文时，游戏信息显示从国际服获取的内容
                foreach (var info in gameInfos)
                {
                    if (info.GameBiz.IsGlobalServer())
                    {
                        list.Add(new GameBizDisplay { GameInfo = info });
                    }
                }
            }

            // 分类每个游戏的服务器信息
            foreach (var item in list)
            {
                string game = item.GameInfo.GameBiz.Game;
                foreach (string suffix in (string[])["_cn", "_global", "_bilibili"])
                {
                    GameBiz biz = game + suffix;
                    if (biz.IsKnown())
                    {
                        var server = new GameBizIcon(biz)
                        {
                            IsPinned = GameBizIcons.Any(x => x.GameBiz == biz),
                        };
                        item.Servers.Add(server);
                    }
                    else if (gameInfos.FirstOrDefault(x => x.GameBiz == biz) is GameInfo info)
                    {
                        var server = new GameBizIcon(info)
                        {
                            IsPinned = GameBizIcons.Any(x => x.GameBiz == biz),
                        };
                        item.Servers.Add(server);
                    }
                }
            }
            GameBizDisplays = new(list);
        }
        catch { }
    }



    /// <summary>
    /// 更新所有游戏服务器信息，更新游戏图标
    /// </summary>
    /// <returns></returns>
    private async Task UpdateGameInfoAsync()
    {
        try
        {
            List<GameInfo> gameInfos = await _hoyoplayService.UpdateGameInfoListAsync();
            InitializeGameServerArea(gameInfos);
            foreach (GameBizIcon icon in GameBizIcons)
            {
                if (icon.GameBiz.IsKnown())
                {
                    icon.UpdateInfo();
                }
                else
                {
                    GameInfo info = await _hoyoplayService.GetGameInfoAsync(icon.GameId);
                    icon.UpdateInfo(info);
                }
            }
            await InitializeInstalledGamesCommand.ExecuteAsync(null);
        }
        catch { }
    }




    /// <summary>
    /// 游戏服务器选择区域是否可见
    /// </summary>
    private bool _isGameBizDisplayPressed = false;


    /// <summary>
    /// 鼠标点击游戏服务器选择区域，显示游戏服务器选择菜单
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Grid_GameBizDisplay_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement ele)
        {
            e.Handled = true;
            _isGameBizDisplayPressed = !_isGameBizDisplayPressed;
            FlyoutBase.GetAttachedFlyout(ele)?.ShowAt(ele, new FlyoutShowOptions
            {
                Placement = FlyoutPlacementMode.Bottom,
                ShowMode = FlyoutShowMode.Transient,
            });
        }
    }


    /// <summary>
    /// 鼠标移入到游戏服务器选择区域
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Grid_GameBizDisplay_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement ele && _isGameBizDisplayPressed)
        {
            FlyoutBase.GetAttachedFlyout(ele)?.ShowAt(ele, new FlyoutShowOptions
            {
                Placement = FlyoutPlacementMode.Bottom,
                ShowMode = FlyoutShowMode.Transient,
            });
        }
    }


    /// <summary>
    /// 点击服务器图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_GameServer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement fe && fe.DataContext is GameBizIcon server)
            {
                if (CurrentGameBizIcon is not null)
                {
                    CurrentGameBizIcon.IsSelected = false;
                }

                if (GameBizIcons.FirstOrDefault(x => x.GameId == server.GameId) is GameBizIcon icon)
                {
                    CurrentGameBizIcon = icon;
                    CurrentGameBiz = icon.GameBiz;
                    CurrentGameId = icon.GameId;
                    icon.IsSelected = true;
                }
                else
                {
                    CurrentGameBizIcon = server;
                    server.IsSelected = true;
                }

                CurrentGameChanged?.Invoke(this, (server.GameId, false));
                // 关闭弹出的服务器选择菜单
                if (VisualTreeHelper.GetOpenPopupsForXamlRoot(this.XamlRoot).FirstOrDefault() is Popup popup)
                {
                    popup.IsOpen = false;
                }
                HideFullBackground();
                _isGameBizDisplayPressed = false;
                AppSetting.CurrentGameBiz = server.GameBiz;
            }
        }
        catch { }
    }


    /// <summary>
    /// 固定游戏服务器图标到待选区
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_PinGameBiz_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is GameBizIcon server)
        {
            var biz = server.GameBiz;
            if (GameBizIcons.FirstOrDefault(x => x.GameBiz == biz) is GameBizIcon icon)
            {
                GameBizIcons.Remove(icon);
                server.IsPinned = false;
            }
            else
            {
                GameBizIcons.Add(server);
                server.IsPinned = true;
            }
        }
    }




    #endregion





    #region Installed Games


    /// <summary>
    /// 已安装游戏的实际占用空间
    /// </summary>
    public string? InstalledGamesActualSize { get; set => SetProperty(ref field, value); }


    /// <summary>
    /// 已安装游戏的总空间
    /// </summary>
    public string? InstalledGamesSavedSize { get; set => SetProperty(ref field, value); }




    public ObservableCollection<GameBizIcon> InstalledGames { get; set; } = new();



    private CancellationTokenSource? _initializeInstalledGamesCancellationTokenSource;


    /// <summary>
    /// 初始化已安装游戏列表，计算已安装游戏的实际占用空间
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task InitializeInstalledGamesAsync()
    {
        const double GB = 1 << 30;
        try
        {
            _initializeInstalledGamesCancellationTokenSource?.Cancel();
            _initializeInstalledGamesCancellationTokenSource = new();
            CancellationToken token = _initializeInstalledGamesCancellationTokenSource.Token;

            InstalledGames.Clear();
            InstalledGamesActualSize = null;
            InstalledGamesSavedSize = null;
            List<FileInfo> files = new();

            foreach (GameBizDisplay display in GameBizDisplays)
            {
                List<FileInfo> _duplicateFiles = new();
                int serverCount = 0;
                foreach (GameBizIcon server in display.Servers)
                {
                    string? installPath = _gameLauncherService.GetGameInstallPath(server.GameId);
                    if (Directory.Exists(installPath))
                    {
                        server.InstallPath = installPath;
                        var _files = new DirectoryInfo(installPath).EnumerateFiles("*", SearchOption.AllDirectories).ToList();
                        server.TotalSize = _files.Sum(x => x.Length);
                        InstalledGames.Add(server);
                        if (_files.Count() > 0)
                        {
                            serverCount++;
                            _duplicateFiles.AddRange(_files);
                        }
                    }
                }
                if (serverCount > 1)
                {
                    files.AddRange(_duplicateFiles);
                }
            }
            long totalSize = InstalledGames.Sum(x => x.TotalSize);
            InstalledGamesActualSize = $"{totalSize / GB:F2}GB";

            if (token.IsCancellationRequested)
            {
                return;
            }

            if (files.Count > 0)
            {
                (long fileSize, long actualSize) = await Task.Run(() =>
                {
                    long size = 0;
                    Dictionary<string, long> dic = new();
                    foreach (var file in files)
                    {
                        size += file.Length;
                        using var handle = File.OpenHandle(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                        var idInfo = Kernel32.GetFileInformationByHandleEx<Kernel32.FILE_ID_INFO>(handle, Kernel32.FILE_INFO_BY_HANDLE_CLASS.FileIdInfo);
                        var idInfoBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref idInfo, 1));
                        dic[Convert.ToHexString(idInfoBytes)] = file.Length;
                    }
                    return (size, dic.Values.Sum());
                }, token);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                InstalledGamesActualSize = $"{(totalSize - fileSize + actualSize) / GB:F2}GB";
                InstalledGamesSavedSize = $"{(fileSize - actualSize) / GB:F2}GB";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }





    [RelayCommand]
    public void AutoSearchInstalledGames()
    {
        try
        {
            // todo 从注册表自动搜索已安装的游戏
            //var service = AppService.GetService<GameLauncherService>();
            //var sb = new StringBuilder();
            //foreach (GameBiz biz in GameBiz.AllGameBizs)
            //{
            //    if (service.IsGameExeExistsAsync(biz))
            //    {
            //        sb.Append(biz.ToString());
            //        sb.Append(',');
            //    }
            //}
            //AppSetting.SelectedGameBizs = sb.ToString().TrimEnd(',');
            //InitializeGameSelector();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }




    /// <summary>
    /// 防止点击已安装游戏列表时，触发 <see cref="Border_FullBackground_Tapped(object, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs)"/>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Expander_InstalledGamesActualSize_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }



    #endregion







}


