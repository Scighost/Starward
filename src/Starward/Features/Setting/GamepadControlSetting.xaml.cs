using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Starward.Features.GamepadControl;
using Starward.Frameworks;


namespace Starward.Features.Setting;

public sealed partial class GamepadControlSetting : PageBase
{

    private readonly ILogger<GamepadControlSetting> _logger = AppConfig.GetLogger<GamepadControlSetting>();




    public GamepadControlSetting()
    {
        InitializeComponent();
        DetectedGameInputRedist();
    }


    /// <summary>
    /// 模拟输入
    /// </summary>
    public bool EnableGamepadSimulateInput
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                GamepadController.EnableGamepadSimulateInput = value;
                AppConfig.EnableGamepadSimulateInput = value;
            }
        }
    } = GamepadController.EnableGamepadSimulateInput;




    private void DetectedGameInputRedist()
    {
        try
        {
            if (GamepadController.GameInputRedistOutdate)
            {
                TextBlock_GameInputRedistOutdate.Visibility = Visibility.Visible;
            }
            else if (GamepadController.NeedInstallGameInputRedist)
            {
                TextBlock_NeedInstallGameInputRedistOnWin10.Visibility = Visibility.Visible;
            }
            else if (!GamepadController.Initialized)
            {
                TextBlock_GameInputInitializeFailed.Visibility = Visibility.Collapsed;
            }
            if (GamepadController.Initialized && GamepadController.GameInputRedistInstalled)
            {
                TextBlock_GameInputRedistInstalled.Visibility = Visibility.Visible;
            }
        }
        catch { }
    }



    #region Guide Button Mapping


    public int GamepadGuideButtonMode
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                GamepadController.GamepadGuideButtonMode = value;
                AppConfig.GamepadGuideButtonMode = value;
                if (value is 2)
                {
                    GamepadGuideButtonModeIsKeyboard = true;
                }
                else
                {
                    GamepadGuideButtonModeIsKeyboard = false;
                    GamepadGuideButtonKeyTextEditSuccess = false;
                }
            }
        }
    } = GamepadController.GamepadGuideButtonMode;


    public string GamepadGuideButtonKeyText
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                if (GamepadController.SetGamepadGuideButtonMapKeys(value, out string? error))
                {
                    GamepadGuideButtonKeyTextEditSuccess = true;
                    GamepadGuideButtonKeyTextEditError = "";
                }
                else
                {
                    GamepadGuideButtonKeyTextEditSuccess = false;
                    GamepadGuideButtonKeyTextEditError = $"{Lang.GamepadControlSetting_UnrecognizedKey}: {error}";
                }
            }
        }
    } = GamepadController.GetGamepadGuideButtonMapKeysText();


    public bool GamepadGuideButtonModeIsKeyboard { get; set => SetProperty(ref field, value); } = GamepadController.GamepadGuideButtonMode is 2;

    public bool GamepadGuideButtonKeyTextEditSuccess { get; set => SetProperty(ref field, value); }

    public string GamepadGuideButtonKeyTextEditError { get; set => SetProperty(ref field, value); }


    #endregion



    #region Share Button Mapping


    public int GamepadShareButtonMode
    {
        get => field;
        set
        {
            if (SetProperty(ref field, value))
            {
                GamepadController.GamepadShareButtonMode = value;
                AppConfig.GamepadShareButtonMode = value;
                if (value is 2)
                {
                    GamepadShareButtonModeIsKeyboard = true;
                }
                else
                {
                    GamepadShareButtonModeIsKeyboard = false;
                    GamepadShareButtonKeyTextEditSuccess = false;
                }
            }
        }
    } = GamepadController.GamepadShareButtonMode;


    public string GamepadShareButtonKeyText
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                if (GamepadController.SetGamepadShareButtonMapKeys(value, out string? error))
                {
                    GamepadShareButtonKeyTextEditSuccess = true;
                    GamepadShareButtonKeyTextEditError = "";
                }
                else
                {
                    GamepadShareButtonKeyTextEditSuccess = false;
                    GamepadShareButtonKeyTextEditError = $"{Lang.GamepadControlSetting_UnrecognizedKey}: {error}";
                }
            }
        }
    } = GamepadController.GetGamepadShareButtonMapKeysText();


    public bool GamepadShareButtonModeIsKeyboard { get; set => SetProperty(ref field, value); } = GamepadController.GamepadShareButtonMode is 2;

    public bool GamepadShareButtonKeyTextEditSuccess { get; set => SetProperty(ref field, value); }

    public string GamepadShareButtonKeyTextEditError { get; set => SetProperty(ref field, value); }


    #endregion



}
