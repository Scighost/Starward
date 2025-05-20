using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Display;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Features.GameLauncher;
using Starward.Features.GameSelector;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


namespace Starward.Features.GameSetting;

public sealed partial class GameSettingPage : PageBase
{

    private readonly ILogger<GameSettingPage> _logger = AppConfig.GetLogger<GameSettingPage>();

    private readonly GameLauncherService _gameLauncherService = AppConfig.GetService<GameLauncherService>();



    public GameSettingPage()
    {
        this.InitializeComponent();
    }




    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Image_Emoji.Source = CurrentGameBiz.ToGame().Value switch
        {
            GameBiz.bh3 => new BitmapImage(AppConfig.EmojiAI),
            GameBiz.hk4e => new BitmapImage(AppConfig.EmojiPaimon),
            GameBiz.hkrpg => new BitmapImage(AppConfig.EmojiPom),
            GameBiz.nap => new BitmapImage(AppConfig.EmojiBangboo),
            _ => null,
        };
        if (CurrentGameId.GameBiz == GameBiz.bh3_global)
        {
            CurrentGameBiz = CurrentGameId.Id switch
            {
                "g0mMIvshDb" => GameBiz.bh3_jp,
                "uxB4MC7nzC" => GameBiz.bh3_kr,
                "bxPTXSET5t" => GameBiz.bh3_os,
                "wkE5P5WsIf" => GameBiz.bh3_asia,
                _ => GameBiz.bh3_global,
            };
        }
    }


    protected override async void OnLoaded()
    {
        InitializeResolutionItem();
        await InitializeGameSettingAsync();
    }


    protected override void OnUnloaded()
    {
        if (_displayInformation is not null)
        {
            _displayInformation.AdvancedColorInfoChanged -= _displayInformation_AdvancedColorInfoChanged;
            _displayInformation.Dispose();
            _displayInformation = null!;
        }
    }


    public bool IsBaseSettingEnable { get; set => SetProperty(ref field, value); }

    public bool IsLanguageSettingEnable { get; set => SetProperty(ref field, value); }

    public bool IsGraphicsSettingEnable { get; set => SetProperty(ref field, value); }

    public bool IsApplyButtonEnable { get; set => SetProperty(ref field, value); }

    public string ErrorMessage { get; set => SetProperty(ref field, value); } = Lang.GameSettingPage_SettingNotEffect; // 游戏运行时应用的设置无法生效





    public string? StartArgument
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.SetStartArgument(CurrentGameBiz, value);
            }
        }
    }


    public bool EnableFullScreen
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
            }
        }
    }



    public bool EnableCustomResolution { get; set => SetProperty(ref field, value); }


    public int ResolutionWidth
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
            }
        }
    }


    public int ResolutionHeight
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
            }
        }
    }


    public int LanguageIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
            }
        }
    }


    public int StarRailFpsIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
            }
        }
    }


    public bool EnableGenshinHDR
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
            }
        }
    }


    public bool SupportHDR { get; set => SetProperty(ref field, value); }


    private async Task InitializeGameSettingAsync()
    {
        try
        {
            var localVersion = await _gameLauncherService.GetLocalGameVersionAsync(CurrentGameId);
            if (localVersion is null)
            {
                StackPanel_Emoji.Visibility = Visibility.Visible;
                return;
            }
            IsBaseSettingEnable = true;
            if (CurrentGameBiz.ToGame().Value is GameBiz.hk4e or GameBiz.hkrpg)
            {
                IsLanguageSettingEnable = true;
            }
            if (CurrentGameBiz.Game is GameBiz.hkrpg)
            {
                IsGraphicsSettingEnable = true;
                StackPanel_StarRailFPS.Visibility = Visibility.Visible;
                StarRailFpsIndex = GameSettingService.GetStarRailFPSIndex(CurrentGameBiz);
            }
            if (CurrentGameBiz.Game is GameBiz.hk4e)
            {
                IsGraphicsSettingEnable = true;
                StackPanel_GenshinHDR.Visibility = Visibility.Visible;
                EnableGenshinHDR = AppConfig.EnableGenshinHDR;
                _displayInformation = DisplayInformation.CreateForWindowId(this.XamlRoot.GetAppWindow().Id);
                _displayInformation.AdvancedColorInfoChanged += _displayInformation_AdvancedColorInfoChanged;
                SupportHDR = _displayInformation.GetAdvancedColorInfo().IsAdvancedColorKindAvailable(DisplayAdvancedColorKind.HighDynamicRange);
            }
            StartArgument = AppConfig.GetStartArgument(CurrentGameBiz);
            var resolutionSetting = GameSettingService.GetGameResolutionSetting(CurrentGameBiz);
            if (resolutionSetting != null)
            {
                EnableFullScreen = resolutionSetting.IsFullScreen;
                if (resolutionSetting.Width * resolutionSetting.Height > 0)
                {
                    ResolutionWidth = resolutionSetting.Width;
                    ResolutionHeight = resolutionSetting.Height;
                    EnableCustomResolution = !UpdateResolutionComboBoxSelection(ResolutionWidth, ResolutionHeight);
                }
                else
                {
                    ComboBox_Resolution.SelectedIndex = 0;
                }
            }
            else
            {
                ComboBox_Resolution.SelectedIndex = 0;
            }
            if (IsLanguageSettingEnable)
            {
                var langSetting = GameSettingService.GetGameVoiceLanguageSetting(CurrentGameBiz);
                if (langSetting != null)
                {
                    LanguageIndex = langSetting.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize Game Setting");
        }
        finally
        {
            IsApplyButtonEnable = false;
        }
    }



    private void InitializeResolutionItem()
    {
        var display = DisplayArea.GetFromWindowId(this.XamlRoot.ContentIslandEnvironment.AppWindowId, DisplayAreaFallback.Nearest);
        var width = display.OuterBounds.Width;
        var height = display.OuterBounds.Height;
        var list = Resolutions.Where(x => x.Width <= width && x.Height <= height).ToList();
        if (list.Count == 0)
        {
            list.Add((width, height));
        }
        else
        {
            if (list[0].Width != width || list[0].Height != height)
            {
                list.Add((width, height));
            }
        }
        foreach (var item in list)
        {
            ComboBox_Resolution.Items.Add(new ComboBoxItem
            {
                Content = $"{item.Width} × {item.Height}",
            });
        }
    }




    private static List<(int Width, int Height)> Resolutions = new List<(int Width, int Height)>()
    {
        (3840 , 2160),
        (2560 , 1600),
        (2560 , 1440),
        (2048 , 1536),
        (1920 , 1440),
        (1920 , 1200),
        (1920 , 1080),
        (1680 , 1050),
        (1600 , 1200),
        (1440 , 900 ),
        (1280 , 960 ),
        (1280 , 800 ),
        (1280 , 720 ),
        (1152 , 864 ),
        (1024 , 768 ),
        (800  , 600 ),
        (640  , 480 ),
    };



    private void ComboBox_Resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is ComboBoxItem item)
        {
            if (item.Content is string str)
            {
                var split = str.Split('×', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (split.Length == 2)
                {
                    if (int.TryParse(split[0], out int width) && int.TryParse(split[1], out int height))
                    {
                        ResolutionWidth = width;
                        ResolutionHeight = height;
                        IsApplyButtonEnable = true;
                    }
                }
            }
        }
    }



    private bool UpdateResolutionComboBoxSelection(int width, int height)
    {
        foreach (ComboBoxItem item in ComboBox_Resolution.Items)
        {
            if (item.Content is string str)
            {
                var split = str.Split('×', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (split.Length == 2)
                {
                    if (int.TryParse(split[0], out int _width) && int.TryParse(split[1], out int _height))
                    {
                        if (width == _width && height == _height)
                        {
                            ComboBox_Resolution.SelectedItem = item;
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }


    [RelayCommand]
    private async Task OpenGenshinHDRLumianceSettingWindow()
    {
        try
        {
            await new GenshinHDRLuminanceSettingDialog { XamlRoot = this.XamlRoot, CurrentGameBiz = this.CurrentGameBiz }.ShowAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            WeakReferenceMessenger.Default.Send(new MainWindowDragRectAdaptToGameIconMessage());
        }
    }



    [RelayCommand]
    private void ApplySetting()
    {
        try
        {
            if (IsBaseSettingEnable)
            {
                if (ResolutionWidth <= 0 || ResolutionHeight <= 0)
                {
                    // 分辨率必须大于0
                    ErrorMessage = Lang.GameSettingPage_ResolutionMustBeGreaterThan0;
                    return;
                }
                var model = new GraphicsSettings_PCResolution_h431323223
                {
                    IsFullScreen = EnableFullScreen,
                    Width = ResolutionWidth,
                    Height = ResolutionHeight,
                };
                GameSettingService.SetGameResolutionSetting(CurrentGameBiz, model);
            }
            if (IsLanguageSettingEnable)
            {
                GameSettingService.SetGameVoiceLanguageSetting(CurrentGameBiz, LanguageIndex);
            }
            if (IsGraphicsSettingEnable)
            {
                if (CurrentGameBiz.Game is GameBiz.hkrpg)
                {
                    GameSettingService.SetStarRailFPSIndex(CurrentGameBiz, StarRailFpsIndex);
                }
                if (CurrentGameBiz.Game is GameBiz.hk4e)
                {
                    AppConfig.EnableGenshinHDR = EnableGenshinHDR;
                    GameSettingService.SetGenshinEnableHDR(CurrentGameBiz, EnableGenshinHDR);
                }
            }
            // 游戏运行时应用的设置无法生效
            ErrorMessage = Lang.GameSettingPage_SettingNotEffect;
            IsApplyButtonEnable = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _logger.LogError(ex, "Apply Setting");
        }
    }



    private DisplayInformation _displayInformation;

    private void _displayInformation_AdvancedColorInfoChanged(DisplayInformation sender, object args)
    {
        try
        {
            SupportHDR = sender.GetAdvancedColorInfo().IsAdvancedColorKindAvailable(DisplayAdvancedColorKind.HighDynamicRange);
        }
        catch { }
    }



}
