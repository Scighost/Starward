using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Starward.Core;
using Starward.Models.GameSetting;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Starward.Services;

internal class GameSettingService
{


    private const string GENERAL_DATA_h2389025596 = "GENERAL_DATA_h2389025596";
    private const string Screenmanager_Is_Fullscreen_mode_h3981298716 = "Screenmanager Is Fullscreen mode_h3981298716";
    private const string Screenmanager_Resolution_Height_h2627697771 = "Screenmanager Resolution Height_h2627697771";
    private const string Screenmanager_Resolution_Width_h182942802 = "Screenmanager Resolution Width_h182942802";


    private const string AudioSettings_BGMVolume_h240914409 = "AudioSettings_BGMVolume_h240914409";
    private const string AudioSettings_MasterVolume_h1622207037 = "AudioSettings_MasterVolume_h1622207037";
    private const string AudioSettings_SFXVolume_h2753520268 = "AudioSettings_SFXVolume_h2753520268";
    private const string AudioSettings_VOVolume_h805685304 = "AudioSettings_VOVolume_h805685304";
    private const string GraphicsSettings_GraphicsQuality_h523255858 = "GraphicsSettings_GraphicsQuality_h523255858";
    private const string GraphicsSettings_Model_h2986158309 = "GraphicsSettings_Model_h2986158309";
    private const string GraphicsSettings_PCResolution_h431323223 = "GraphicsSettings_PCResolution_h431323223";
    // cn en jp kr
    private const string LanguageSettings_LocalAudioLanguage_h882585060 = "LanguageSettings_LocalAudioLanguage_h882585060";
    private const string LanguageSettings_LocalTextLanguage_h2764291023 = "LanguageSettings_LocalTextLanguage_h2764291023";
    // 1 full, 3 window
    private const string Screenmanager_Fullscreen_mode_h3630240806 = "Screenmanager Fullscreen mode_h3630240806";


    private const string GENERAL_DATA_V2_ScreenSettingData_h1916288658 = "GENERAL_DATA_V2_ScreenSettingData_h1916288658";




    private readonly ILogger<GameSettingService> _logger;


    public GameSettingService(ILogger<GameSettingService> logger)
    {
        _logger = logger;
    }




    public GraphicsSettings_PCResolution_h431323223? GetGameResolutionSetting(GameBiz biz)
    {
        var keyPath = biz.GetGameRegistryKey();
        if (biz.ToGame() is GameBiz.Honkai3rd or GameBiz.StarRail)
        {
            var keyName = biz.ToGame() switch
            {
                GameBiz.Honkai3rd => GENERAL_DATA_V2_ScreenSettingData_h1916288658,
                GameBiz.StarRail => GraphicsSettings_PCResolution_h431323223,
                _ => ""
            };
            var data = Registry.GetValue(keyPath, keyName, null) as byte[];
            if (data is not null)
            {
                var str = Encoding.UTF8.GetString(data).TrimEnd('\0');
                return JsonSerializer.Deserialize<GraphicsSettings_PCResolution_h431323223>(str);
            }
        }
        if (biz.ToGame() is GameBiz.GenshinImpact)
        {
            var fullScreen = (int)(Registry.GetValue(keyPath, Screenmanager_Is_Fullscreen_mode_h3981298716, 0) ?? 0) != 0;
            var width = (int)(Registry.GetValue(keyPath, Screenmanager_Resolution_Width_h182942802, 0) ?? 0);
            var height = (int)(Registry.GetValue(keyPath, Screenmanager_Resolution_Height_h2627697771, 0) ?? 0);
            if (width * height > 0)
            {
                return new GraphicsSettings_PCResolution_h431323223 { Width = width, Height = height, IsFullScreen = fullScreen };
            }
        }
        _logger.LogInformation("Resolution of game {biz} is null", biz);
        return null;
    }



    public void SetGameResolutionSetting(GameBiz biz, GraphicsSettings_PCResolution_h431323223 model)
    {
        var keyPath = biz.GetGameRegistryKey();
        if (biz.ToGame() is GameBiz.Honkai3rd)
        {
            var str = JsonSerializer.Serialize(model) + "\0";
            Registry.SetValue(keyPath, GENERAL_DATA_V2_ScreenSettingData_h1916288658, Encoding.UTF8.GetBytes(str));
            Registry.SetValue(keyPath, Screenmanager_Is_Fullscreen_mode_h3981298716, model.IsFullScreen ? 1 : 0);
            Registry.SetValue(keyPath, Screenmanager_Resolution_Width_h182942802, model.Width);
            Registry.SetValue(keyPath, Screenmanager_Resolution_Height_h2627697771, model.Height);
        }
        if (biz.ToGame() is GameBiz.StarRail)
        {
            var str = JsonSerializer.Serialize(model) + "\0";
            Registry.SetValue(keyPath, GraphicsSettings_PCResolution_h431323223, Encoding.UTF8.GetBytes(str));
            Registry.SetValue(keyPath, Screenmanager_Fullscreen_mode_h3630240806, model.IsFullScreen ? 1 : 3);
            Registry.SetValue(keyPath, Screenmanager_Resolution_Width_h182942802, model.Width);
            Registry.SetValue(keyPath, Screenmanager_Resolution_Height_h2627697771, model.Height);
        }
        if (biz.ToGame() is GameBiz.GenshinImpact)
        {
            Registry.SetValue(keyPath, Screenmanager_Is_Fullscreen_mode_h3981298716, model.IsFullScreen ? 1 : 0);
            Registry.SetValue(keyPath, Screenmanager_Resolution_Width_h182942802, model.Width);
            Registry.SetValue(keyPath, Screenmanager_Resolution_Height_h2627697771, model.Height);
        }
    }




    public int? GetGameVoiceLanguageSetting(GameBiz biz)
    {
        var keyPath = biz.GetGameRegistryKey();
        if (biz.ToGame() is GameBiz.GenshinImpact)
        {
            var data = Registry.GetValue(keyPath, GENERAL_DATA_h2389025596, null) as byte[];
            if (data is not null)
            {
                var str = Encoding.UTF8.GetString(data).TrimEnd('\0');
                var node = JsonNode.Parse(str);
                var value = (int)(node?["deviceVoiceLanguageType"] ?? -1);
                return value >= 0 ? value : null;
            }
        }
        if (biz.ToGame() is GameBiz.StarRail)
        {
            var data = Registry.GetValue(keyPath, LanguageSettings_LocalAudioLanguage_h882585060, null) as byte[];
            if (data is not null)
            {
                var str = Encoding.UTF8.GetString(data).TrimEnd('\0');
                return str switch
                {
                    "cn" => 0,
                    "en" => 1,
                    "jp" => 2,
                    "kr" => 3,
                    _ => null,
                };
            }
        }
        _logger.LogInformation("Voice language of game {biz} is null", biz);
        return null;
    }


    public void SetGameVoiceLanguageSetting(GameBiz biz, int lang)
    {
        var keyPath = biz.GetGameRegistryKey();
        if (biz.ToGame() is GameBiz.GenshinImpact)
        {
            var data = Registry.GetValue(keyPath, GENERAL_DATA_h2389025596, null) as byte[];
            JsonNode? node = null;
            if (data is not null)
            {
                var str = Encoding.UTF8.GetString(data).TrimEnd('\0');
                node = JsonNode.Parse(str);
                if (node != null)
                {
                    node["deviceVoiceLanguageType"] = lang;
                }
            }
            if (node != null)
            {
                var str = node.ToJsonString() + "\0";
                Registry.SetValue(keyPath, GENERAL_DATA_h2389025596, Encoding.UTF8.GetBytes(str));
            }
            else
            {
                Registry.SetValue(keyPath, GENERAL_DATA_h2389025596, Encoding.UTF8.GetBytes($"{{\"deviceVoiceLanguageType\": {lang}}}\0"));
            }
        }
        if (biz.ToGame() is GameBiz.StarRail)
        {
            var str = lang switch
            {
                0 => "cn\0",
                1 => "en\0",
                2 => "jp\0",
                3 => "kr\0",
                _ => null,
            };
            if (!string.IsNullOrWhiteSpace(str))
            {
                Registry.SetValue(keyPath, LanguageSettings_LocalAudioLanguage_h882585060, Encoding.UTF8.GetBytes(str));
            }
        }
    }



    public int? GetGraphicsQualitySetting(GameBiz biz)
    {
        var keyPath = biz.GetGameRegistryKey();
        if (biz.ToGame() is GameBiz.StarRail)
        {
            var value = (int)(Registry.GetValue(keyPath, GraphicsSettings_GraphicsQuality_h523255858, 0) ?? -1);
            return value >= 0 ? value : null;
        }
        _logger.LogInformation("Graphics quality of game {biz} is null", biz);
        return null;
    }



    public void SetGraphicsQualitySetting(GameBiz biz, int value)
    {
        if (biz.ToGame() is GameBiz.StarRail)
        {
            var keyPath = biz.GetGameRegistryKey();
            Registry.SetValue(keyPath, GraphicsSettings_GraphicsQuality_h523255858, value);
        }
    }



    public GraphicsSettings_Model_h2986158309? GetGraphicsSettingModel(GameBiz biz)
    {
        var keyPath = biz.GetGameRegistryKey();
        if (biz.ToGame() is GameBiz.StarRail)
        {
            var data = Registry.GetValue(keyPath, GraphicsSettings_Model_h2986158309, null) as byte[];
            if (data is not null)
            {
                var str = Encoding.UTF8.GetString(data).TrimEnd('\0');
                return JsonSerializer.Deserialize<GraphicsSettings_Model_h2986158309>(str);
            }
        }
        _logger.LogInformation("Graphics setting model of game {biz} is null", biz);
        return null;
    }



    public void SetGraphicsSettingModel(GameBiz biz, GraphicsSettings_Model_h2986158309? model)
    {
        if (biz.ToGame() is GameBiz.StarRail)
        {
            var keyPath = biz.GetGameRegistryKey();
            var str = JsonSerializer.Serialize(model) + "\0";
            Registry.SetValue(keyPath, GraphicsSettings_Model_h2986158309, Encoding.UTF8.GetBytes(str));
        }
    }




}
