using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Features.GameLauncher;
using Starward.Frameworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Starward.Features.GameSetting;

public sealed partial class GameSettingPage : PageBase
{

    private readonly ILogger<GameSettingPage> _logger = AppService.GetLogger<GameSettingPage>();

    private readonly GameLauncherService _gameLauncherService = AppService.GetService<GameLauncherService>();

    private readonly GameSettingService _gameSettingService = AppService.GetService<GameSettingService>();



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
    }


    protected override async void OnLoaded()
    {
        InitializeResolutionItem();
        await InitializeGameSettingAsync();
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
                AppSetting.SetStartArgument(CurrentGameBiz, value);
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



    public int GraphicsQualityIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
                GraphicsQualityChanged();
            }
        }
    }


    public double RenderScale
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
                GraphicsQualityChanged();
            }
        }
    }



    public int FpsIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
                GraphicsQualityChanged();
            }
        }
    }


    public int VSyncIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
                GraphicsQualityChanged();
            }
        }
    }


    public int AAModeIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
                GraphicsQualityChanged();
            }
        }
    }


    public int ShadowQualityIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
                GraphicsQualityChanged();
            }
        }
    }


    public int ReflectionQualityIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
                GraphicsQualityChanged();
            }
        }
    }


    public int CharacterQualityIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
                GraphicsQualityChanged();
            }
        }
    }


    public int EnvDetailQualityIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
                GraphicsQualityChanged();
            }
        }
    }


    public int BloomQualityIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
                GraphicsQualityChanged();
            }
        }
    }


    public int LightQualityIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                IsApplyButtonEnable = true;
                GraphicsQualityChanged();
            }
        }
    }





    private async Task InitializeGameSettingAsync()
    {
        try
        {
            var localVersion = await _gameLauncherService.GetLocalGameVersionAsync(CurrentGameId);
            if (localVersion is null)
            {
                if (CurrentGameBiz == GameBiz.clgm_cn)
                {
                    TextBlock_GameNotInstalled.Text = Lang.GameSettingPage_FeatureNotSupported;
                }
                StackPanel_Emoji.Visibility = Visibility.Visible;
                return;
            }
            IsBaseSettingEnable = true;
            if (CurrentGameBiz.ToGame().Value is GameBiz.hk4e or GameBiz.hkrpg)
            {
                IsLanguageSettingEnable = true;
            }
            if (CurrentGameBiz.ToGame() == GameBiz.hkrpg)
            {
                IsGraphicsSettingEnable = true;
            }
            StartArgument = AppSetting.GetStartArgument(CurrentGameBiz);
            var resolutionSetting = _gameSettingService.GetGameResolutionSetting(CurrentGameBiz);
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
                var langSetting = _gameSettingService.GetGameVoiceLanguageSetting(CurrentGameBiz);
                if (langSetting != null)
                {
                    LanguageIndex = langSetting.Value;
                }
            }
            if (IsGraphicsSettingEnable)
            {
                GraphicsSettings_Model_h2986158309? model = null;
                var quality = _gameSettingService.GetGraphicsQualitySetting(CurrentGameBiz);
                if (quality != null)
                {
                    GraphicsQualityIndex = quality.Value;
                    if (quality == 0)
                    {
                        model = _gameSettingService.GetGraphicsSettingModel(CurrentGameBiz);
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
                _gameSettingService.SetGameResolutionSetting(CurrentGameBiz, model);
            }
            if (IsLanguageSettingEnable)
            {
                _gameSettingService.SetGameVoiceLanguageSetting(CurrentGameBiz, LanguageIndex);
            }
            if (IsGraphicsSettingEnable)
            {
                _gameSettingService.SetGraphicsQualitySetting(CurrentGameBiz, GraphicsQualityIndex);
                if (GraphicsQualityIndex == 0)
                {
                    var model = GraphicsSettingToSettingModel();
                    _gameSettingService.SetGraphicsSettingModel(CurrentGameBiz, model);
                }
                else
                {
                    _gameSettingService.SetGraphicsSettingModel(CurrentGameBiz, null);
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
