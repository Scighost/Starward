using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Models.GameSetting;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class GameSettingPage : PageBase
{

    private readonly ILogger<GameSettingPage> _logger = AppConfig.GetLogger<GameSettingPage>();

    private readonly GameResourceService _gameResourceService = AppConfig.GetService<GameResourceService>();

    private readonly GameSettingService _gameSettingService = AppConfig.GetService<GameSettingService>();



    public GameSettingPage()
    {
        this.InitializeComponent();
    }




    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is GameBiz biz)
        {
            gameBiz = biz;
            Image_Emoji.Source = gameBiz.ToGame() switch
            {
                GameBiz.GenshinImpact => new BitmapImage(AppConfig.EmojiPaimon),
                GameBiz.StarRail => new BitmapImage(AppConfig.EmojiPom),
                GameBiz.Honkai3rd => new BitmapImage(AppConfig.EmojiAI),
                _ => null,
            };
        }
    }



    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (AppConfig.EnableNavigationViewLeftCompact)
        {
            Grid_ApplyBackground.CornerRadius = new CornerRadius(8, 0, 0, 0);
        }
        InitializeResolutionItem();
        await InitializeGameSettingAsync();
    }



    private GameBiz gameBiz;

    [ObservableProperty]
    public bool isBaseSettingEnable;
    [ObservableProperty]
    public bool isLanguageSettingEnable;
    [ObservableProperty]
    public bool isGraphicsSettingEnable;

    [ObservableProperty]
    private bool isApplyButtonEnable;

    [ObservableProperty]
    private string errorMessage = Lang.GameSettingPage_SettingNotEffect; // 游戏运行时应用的设置无法生效


    [ObservableProperty]
    private string? startArgument;
    partial void OnStartArgumentChanged(string? value)
    {
        AppConfig.SetStartArgument(gameBiz, value);
    }

    [ObservableProperty]
    private bool enableFullScreen;
    partial void OnEnableFullScreenChanged(bool value)
    {
        IsApplyButtonEnable = true;
    }


    [ObservableProperty]
    private bool enableCustomResolution;


    [ObservableProperty]
    private int resolutionWidth;
    partial void OnResolutionWidthChanged(int value)
    {
        IsApplyButtonEnable = true;
    }


    [ObservableProperty]
    private int resolutionHeight;
    partial void OnResolutionHeightChanged(int value)
    {
        IsApplyButtonEnable = true;
    }


    [ObservableProperty]
    private int languageIndex;
    partial void OnLanguageIndexChanged(int value)
    {
        IsApplyButtonEnable = true;
    }



    [ObservableProperty]
    private int graphicsQualityIndex;
    partial void OnGraphicsQualityIndexChanged(int value)
    {
        IsApplyButtonEnable = true;
        GraphicsQualityChanged();
    }

    [ObservableProperty]
    private double renderScale;
    partial void OnRenderScaleChanged(double value)
    {
        IsApplyButtonEnable = true;
        GraphicsSettingChanged();
    }

    [ObservableProperty]
    private int fpsIndex;
    partial void OnFpsIndexChanged(int value)
    {
        IsApplyButtonEnable = true;
        GraphicsSettingChanged();
    }

    [ObservableProperty]
    private int vSyncIndex;
    partial void OnVSyncIndexChanged(int value)
    {
        IsApplyButtonEnable = true;
        GraphicsSettingChanged();
    }

    [ObservableProperty]
    private int aAModeIndex;
    partial void OnAAModeIndexChanged(int value)
    {
        IsApplyButtonEnable = true;
        GraphicsSettingChanged();
    }

    [ObservableProperty]
    private int shadowQualityIndex;
    partial void OnShadowQualityIndexChanged(int value)
    {
        IsApplyButtonEnable = true;
        GraphicsSettingChanged();
    }

    [ObservableProperty]
    private int reflectionQualityIndex;
    partial void OnReflectionQualityIndexChanged(int value)
    {
        IsApplyButtonEnable = true;
        GraphicsSettingChanged();
    }

    [ObservableProperty]
    private int characterQualityIndex;
    partial void OnCharacterQualityIndexChanged(int value)
    {
        IsApplyButtonEnable = true;
        GraphicsSettingChanged();
    }

    [ObservableProperty]
    private int envDetailQualityIndex;
    partial void OnEnvDetailQualityIndexChanged(int value)
    {
        IsApplyButtonEnable = true;
        GraphicsSettingChanged();
    }

    [ObservableProperty]
    private int bloomQualityIndex;
    partial void OnBloomQualityIndexChanged(int value)
    {
        IsApplyButtonEnable = true;
        GraphicsSettingChanged();
    }

    [ObservableProperty]
    private int lightQualityIndex;
    partial void OnLightQualityIndexChanged(int value)
    {
        IsApplyButtonEnable = true;
        GraphicsSettingChanged();
    }





    private async Task InitializeGameSettingAsync()
    {
        try
        {
            (var localVersion, _) = await _gameResourceService.GetLocalGameVersionAndBizAsync(gameBiz);
            if (localVersion is null)
            {
                if (gameBiz is GameBiz.hk4e_cloud)
                {
                    TextBlock_GameNotInstalled.Text = Lang.GameSettingPage_FeatureNotSupported;
                }
                StackPanel_Emoji.Visibility = Visibility.Visible;
                return;
            }
            IsBaseSettingEnable = true;
            if (gameBiz.ToGame() is GameBiz.GenshinImpact or GameBiz.StarRail)
            {
                IsLanguageSettingEnable = true;
            }
            if (gameBiz.ToGame() is GameBiz.StarRail)
            {
                IsGraphicsSettingEnable = true;
            }
            StartArgument = AppConfig.GetStartArgument(gameBiz);
            var resolutionSetting = _gameSettingService.GetGameResolutionSetting(gameBiz);
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
                var langSetting = _gameSettingService.GetGameVoiceLanguageSetting(gameBiz);
                if (langSetting != null)
                {
                    LanguageIndex = langSetting.Value;
                }
            }
            if (IsGraphicsSettingEnable)
            {
                GraphicsSettings_Model_h2986158309? model = null;
                var quality = _gameSettingService.GetGraphicsQualitySetting(gameBiz);
                if (quality != null)
                {
                    GraphicsQualityIndex = quality.Value;
                    if (quality == 0)
                    {
                        model = _gameSettingService.GetGraphicsSettingModel(gameBiz);
                    }
                    else
                    {
                        model = quality.Value switch
                        {
                            1 => GraphicsSettings_Model_h2986158309.VeryLow,
                            2 => GraphicsSettings_Model_h2986158309.Low,
                            3 => GraphicsSettings_Model_h2986158309.Medium,
                            4 => GraphicsSettings_Model_h2986158309.High,
                            5 => GraphicsSettings_Model_h2986158309.VeryHigh,
                            _ => GraphicsSettings_Model_h2986158309.Medium,
                        };
                    }
                }
                if (model is null)
                {
                    GraphicsQualityIndex = 3;
                    model = GraphicsSettings_Model_h2986158309.Medium;
                }
                UpdateGraphicsSetting(model);
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



    private void UpdateGraphicsQuality(GraphicsSettings_Model_h2986158309 model)
    {
        if (model == GraphicsSettings_Model_h2986158309.VeryLow)
        {
            GraphicsQualityIndex = 1;
        }
        else
        if (model == GraphicsSettings_Model_h2986158309.Low)
        {
            GraphicsQualityIndex = 2;
        }
        else
        if (model == GraphicsSettings_Model_h2986158309.Medium)
        {
            GraphicsQualityIndex = 3;
        }
        else
        if (model == GraphicsSettings_Model_h2986158309.High)
        {
            GraphicsQualityIndex = 4;
        }
        else
        if (model == GraphicsSettings_Model_h2986158309.VeryHigh)
        {
            GraphicsQualityIndex = 5;
        }
        else
        {
            GraphicsQualityIndex = 0;
        }
    }


    private void UpdateGraphicsSetting(GraphicsSettings_Model_h2986158309 model)
    {
        RenderScale = model.RenderScale;
        FpsIndex = model.FPS switch
        {
            < 60 => 0,
            60 => 1,
            > 60 => 2,
        };
        VSyncIndex = model.EnableVSync ? 1 : 0;
        AAModeIndex = model.AAMode;
        ShadowQualityIndex = Math.Clamp(model.ShadowQuality - 1, 0, 3);
        ReflectionQualityIndex = Math.Clamp(model.ReflectionQuality - 1, 0, 4);
        CharacterQualityIndex = Math.Clamp(model.CharacterQuality - 2, 0, 2);
        EnvDetailQualityIndex = Math.Clamp(model.EnvDetailQuality - 1, 0, 4);
        LightQualityIndex = Math.Clamp(model.LightQuality - 1, 0, 4);
        BloomQualityIndex = Math.Clamp(model.BloomQuality, 0, 5);
    }



    private GraphicsSettings_Model_h2986158309 GraphicsSettingToSettingModel()
    {
        var model = new GraphicsSettings_Model_h2986158309();
        model.RenderScale = RenderScale;
        model.FPS = FpsIndex switch
        {
            0 => 30,
            1 => 60,
            2 => 120,
            _ => 60,
        };
        model.EnableVSync = VSyncIndex == 1;
        model.AAMode = AAModeIndex;
        model.ShadowQuality = ShadowQualityIndex switch
        {
            0 => 0,
            1 => 2,
            2 => 3,
            3 => 4,
            _ => 3,
        };
        model.ReflectionQuality = ReflectionQualityIndex + 1;
        model.CharacterQuality = CharacterQualityIndex + 2;
        model.EnvDetailQuality = EnvDetailQualityIndex + 1;
        model.LightQuality = LightQualityIndex + 1;
        model.BloomQuality = BloomQualityIndex;
        return model;
    }




    private void GraphicsQualityChanged()
    {
        if (GraphicsQualityIndex != 0)
        {
            var model = GraphicsQualityIndex switch
            {
                1 => GraphicsSettings_Model_h2986158309.VeryLow,
                2 => GraphicsSettings_Model_h2986158309.Low,
                3 => GraphicsSettings_Model_h2986158309.Medium,
                4 => GraphicsSettings_Model_h2986158309.High,
                5 => GraphicsSettings_Model_h2986158309.VeryHigh,
                _ => GraphicsSettings_Model_h2986158309.Medium,
            };
            UpdateGraphicsSetting(model);
        }
    }



    private void GraphicsSettingChanged()
    {
        var model = GraphicsSettingToSettingModel();
        UpdateGraphicsQuality(model);
    }




    private void InitializeResolutionItem()
    {
        var display = DisplayArea.GetFromWindowId(MainWindow.Current.AppWindow.Id, DisplayAreaFallback.Primary);
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
                _gameSettingService.SetGameResolutionSetting(gameBiz, model);
            }
            if (IsLanguageSettingEnable)
            {
                _gameSettingService.SetGameVoiceLanguageSetting(gameBiz, LanguageIndex);
            }
            if (IsGraphicsSettingEnable)
            {
                _gameSettingService.SetGraphicsQualitySetting(gameBiz, GraphicsQualityIndex);
                if (GraphicsQualityIndex == 0)
                {
                    var model = GraphicsSettingToSettingModel();
                    _gameSettingService.SetGraphicsSettingModel(gameBiz, model);
                }
                else
                {
                    _gameSettingService.SetGraphicsSettingModel(gameBiz, null);
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




}
