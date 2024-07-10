using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.StarRail.ApocalypticShadow;
using Starward.Helpers;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Starward.Services.Cache;
using Windows.Storage;
using System.ComponentModel;
using Starward.Controls;
using Windows.Storage.Streams;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.HoyolabToolbox;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class ApocalypticShadowPage : PageBase
{


    private readonly ILogger<ApocalypticShadowPage> _logger = AppConfig.GetLogger<ApocalypticShadowPage>();

    private readonly GameRecordService _gameRecordService = AppConfig.GetService<GameRecordService>();



    public ApocalypticShadowPage()
    {
        this.InitializeComponent();
    }



    private GameRecordRole gameRole;


    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is GameRecordRole role)
        {
            gameRole = role;
        }
    }



    protected override async void OnLoaded()
    {
        await Task.Delay(16);
        InitializeApocalypticShadowInfoData();
    }




    [ObservableProperty]
    private List<ApocalypticShadowInfo> apocalypticShadowList;


    [ObservableProperty]
    private ApocalypticShadowInfo? currentApocalypticShadow;



    private void InitializeApocalypticShadowInfoData()
    {
        try
        {
            CurrentApocalypticShadow = null;
            var list = _gameRecordService.GetApocalypticShadowInfoList(gameRole);
            if (list.Count != 0)
            {
                ApocalypticShadowList = list;
                ListView_ForgottenHall.SelectedIndex = 0;
            }
            else
            {
                Image_Emoji.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Init apocalyptic shadow data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }




    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        try
        {
            if (gameRole is null)
            {
                return;
            }
            await _gameRecordService.RefreshApocalypticShadowInfoAsync(gameRole, 1);
            await _gameRecordService.RefreshApocalypticShadowInfoAsync(gameRole, 2);
            InitializeApocalypticShadowInfoData();
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Refresh apocalyptic shadow data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            HoyolabToolboxPage.HandleMiHoYoApiException(ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Refresh apocalyptic shadow data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            NotificationBehavior.Instance.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh apocalyptic shadow data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            NotificationBehavior.Instance.Error(ex);
        }
    }



    private void ListView_ForgottenHall_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.AddedItems.FirstOrDefault() is ApocalypticShadowInfo info)
            {
                CurrentApocalypticShadow = _gameRecordService.GetApocalypticShadowInfo(gameRole, info.ScheduleId);
                Image_Emoji.Visibility = (CurrentApocalypticShadow?.HasData ?? false) ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selection changed ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }

    private void TextBlock_Deepest_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        TextBlock_Deepest.SetValue(Grid.ColumnSpanProperty, 2);
        TextBlock_Battles.SetValue(Grid.RowProperty, 1);
        TextBlock_Battles.SetValue(Grid.ColumnProperty, 1);
        TextBlock_Battles.SetValue(Grid.ColumnSpanProperty, 2);
    }

    
    public async Task<byte[]?> BossIconAsync(string Icon)
    {
        var grayIcon = MemoryCache.Instance.GetItem<byte[]>($"GrayBossIcon_{Icon}", TimeSpan.FromSeconds(10));
        if (grayIcon is null)
        {
            var file = await FileCacheService.Instance.GetFromCacheAsync(new Uri(Icon));
            if (file is StorageFile)
            {
                CanvasDevice canvasDevice = CanvasDevice.GetSharedDevice();
                CanvasBitmap colorBitmap = await CanvasBitmap.LoadAsync(canvasDevice, file.Path);
                CanvasRenderTarget canvasRenderTarget = new CanvasRenderTarget(canvasDevice, colorBitmap.SizeInPixels.Width, colorBitmap.SizeInPixels.Height, 96);
                var grayscaleEffect = new GrayscaleEffect
                {
                    Source = colorBitmap
                };
                using (var ds = canvasRenderTarget.CreateDrawingSession())
                    ds.DrawImage(grayscaleEffect);
                // 貌似转为BitmapImage只能使用内存流，尽管可能性极低，但也尽量避免内存泄漏
                using (InMemoryRandomAccessStream randomAccessStream = new())
                {
                    await canvasRenderTarget.SaveAsync(randomAccessStream, CanvasBitmapFileFormat.Png);
                    DataReader reader = new DataReader(randomAccessStream.GetInputStreamAt(0));
                    grayIcon = new byte[randomAccessStream.Size];
                    await reader.LoadAsync((uint)randomAccessStream.Size);
                    reader.ReadBytes(grayIcon);
                }
            }
            MemoryCache.Instance.SetItem($"GrayBossIcon_{Icon}", grayIcon);
        }
        return grayIcon;
    }

    private async Task<object> GetBossIconAsync(string bossIcon, bool isDefeated)
    {
        if (isDefeated)
        {
            var iconSource = new BitmapImage();
            // CPU密集型任务防止卡UI线程
            if (await Task.Run(() => BossIconAsync(bossIcon)) is byte[] icon)
            {
                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    using (DataWriter writer = new DataWriter(stream.GetOutputStreamAt(0)))
                    {
                        writer.WriteBytes(icon);
                        await writer.StoreAsync();
                    }
                    await iconSource.SetSourceAsync(stream);
                }
            }
            return iconSource;
        }
        else
            return bossIcon;
    }

    private async void StackPanel_BossIcon_Loading(FrameworkElement sender, object args)
    {
        if (CurrentApocalypticShadow is ApocalypticShadowInfo && sender is StackPanel stackPanel)
        {
            var upperIconSource = await GetBossIconAsync(CurrentApocalypticShadow.UpperBossIcon, CurrentApocalypticShadow.AllFloorDetail.First().Node1.BossDefeated);
            var lowerIconSource = await GetBossIconAsync(CurrentApocalypticShadow.LowerBossIcon, CurrentApocalypticShadow.AllFloorDetail.First().Node2.BossDefeated);

            var UpperIcon = new CachedImage
            {
                IsCacheEnabled = true,
                Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill,
                Height = 42,
                Source = upperIconSource,
            };
            var LowerIcon = new CachedImage
            {
                IsCacheEnabled = true,
                Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill,
                Height = 42,
                Source = lowerIconSource,
            };

            stackPanel.Children.Add(UpperIcon);
            stackPanel.Children.Add(LowerIcon);
        }
    }
}
