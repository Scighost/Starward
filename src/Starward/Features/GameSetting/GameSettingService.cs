using Microsoft.Win32;
using Starward.Core;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Starward.Features.GameSetting;

internal static class GameSettingService
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

    private const string WINDOWS_HDR_ON_h3132281285 = "WINDOWS_HDR_ON_h3132281285";


    public static GraphicsSettings_PCResolution_h431323223? GetGameResolutionSetting(GameBiz biz)
    {
        var keyPath = biz.GetGameRegistryKey();
        if (biz.ToGame().Value is GameBiz.bh3 or GameBiz.hkrpg)
        {
            var keyName = biz.ToGame().Value switch
            {
                GameBiz.bh3 => GENERAL_DATA_V2_ScreenSettingData_h1916288658,
                GameBiz.hkrpg => GraphicsSettings_PCResolution_h431323223,
                _ => ""
            };
            var data = Registry.GetValue(keyPath, keyName, null) as byte[];
            if (data is not null)
            {
                var str = Encoding.UTF8.GetString(data).TrimEnd('\0');
                return JsonSerializer.Deserialize<GraphicsSettings_PCResolution_h431323223>(str);
            }
        }
        if (biz.ToGame() == GameBiz.hk4e)
        {
            var fullScreen = (int)(Registry.GetValue(keyPath, Screenmanager_Is_Fullscreen_mode_h3981298716, 0) ?? 0) != 0;
            var width = (int)(Registry.GetValue(keyPath, Screenmanager_Resolution_Width_h182942802, 0) ?? 0);
            var height = (int)(Registry.GetValue(keyPath, Screenmanager_Resolution_Height_h2627697771, 0) ?? 0);
            if (width * height > 0)
            {
                return new GraphicsSettings_PCResolution_h431323223 { Width = width, Height = height, IsFullScreen = fullScreen };
            }
        }
        if (biz.ToGame() == GameBiz.nap)
        {
            var fullScreen = (int)(Registry.GetValue(keyPath, Screenmanager_Fullscreen_mode_h3630240806, 0) ?? 0) != 3;
            var width = (int)(Registry.GetValue(keyPath, Screenmanager_Resolution_Width_h182942802, 0) ?? 0);
            var height = (int)(Registry.GetValue(keyPath, Screenmanager_Resolution_Height_h2627697771, 0) ?? 0);
            if (width * height > 0)
            {
                return new GraphicsSettings_PCResolution_h431323223 { Width = width, Height = height, IsFullScreen = fullScreen };
            }
        }
        return null;
    }



    public static void SetGameResolutionSetting(GameBiz biz, GraphicsSettings_PCResolution_h431323223 model)
    {
        var keyPath = biz.GetGameRegistryKey();
        if (biz.ToGame() == GameBiz.bh3)
        {
            var str = JsonSerializer.Serialize(model) + "\0";
            Registry.SetValue(keyPath, GENERAL_DATA_V2_ScreenSettingData_h1916288658, Encoding.UTF8.GetBytes(str));
            Registry.SetValue(keyPath, Screenmanager_Is_Fullscreen_mode_h3981298716, model.IsFullScreen ? 1 : 0);
            Registry.SetValue(keyPath, Screenmanager_Resolution_Width_h182942802, model.Width);
            Registry.SetValue(keyPath, Screenmanager_Resolution_Height_h2627697771, model.Height);
        }
        if (biz.ToGame() == GameBiz.hkrpg)
        {
            var str = JsonSerializer.Serialize(model) + "\0";
            Registry.SetValue(keyPath, GraphicsSettings_PCResolution_h431323223, Encoding.UTF8.GetBytes(str));
            Registry.SetValue(keyPath, Screenmanager_Fullscreen_mode_h3630240806, model.IsFullScreen ? 1 : 3);
            Registry.SetValue(keyPath, Screenmanager_Resolution_Width_h182942802, model.Width);
            Registry.SetValue(keyPath, Screenmanager_Resolution_Height_h2627697771, model.Height);
        }
        if (biz.ToGame() == GameBiz.hk4e)
        {
            Registry.SetValue(keyPath, Screenmanager_Is_Fullscreen_mode_h3981298716, model.IsFullScreen ? 1 : 0);
            Registry.SetValue(keyPath, Screenmanager_Resolution_Width_h182942802, model.Width);
            Registry.SetValue(keyPath, Screenmanager_Resolution_Height_h2627697771, model.Height);
        }
        if (biz.ToGame() == GameBiz.nap)
        {
            Registry.SetValue(keyPath, Screenmanager_Fullscreen_mode_h3630240806, model.IsFullScreen ? 1 : 3);
            Registry.SetValue(keyPath, Screenmanager_Resolution_Width_h182942802, model.Width);
            Registry.SetValue(keyPath, Screenmanager_Resolution_Height_h2627697771, model.Height);
        }
    }




    public static int? GetGameVoiceLanguageSetting(GameBiz biz)
    {
        var keyPath = biz.GetGameRegistryKey();
        if (biz.ToGame() == GameBiz.hk4e)
        {
            var data = Registry.GetValue(keyPath, GENERAL_DATA_h2389025596, null) as byte[];
            if (data is not null)
            {
                var str = Encoding.UTF8.GetString(data).TrimEnd('\0');
                var node = JsonNode.Parse(str);
                return node?["deviceVoiceLanguageType"]?.GetValue<int>();
            }
        }
        if (biz.ToGame() == GameBiz.hkrpg)
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
        return null;
    }


    public static void SetGameVoiceLanguageSetting(GameBiz biz, int lang)
    {
        var keyPath = biz.GetGameRegistryKey();
        if (biz.Game is GameBiz.hk4e)
        {
            var data = Registry.GetValue(keyPath, GENERAL_DATA_h2389025596, null) as byte[];
            JsonNode? node = null;
            if (data is not null)
            {
                var str = Encoding.UTF8.GetString(data).TrimEnd('\0');
                node = JsonNode.Parse(str);
                node?["deviceVoiceLanguageType"] = lang;
            }
            string value = $"{node?.ToJsonString() ?? ($$"""{"deviceVoiceLanguageType":{{lang}}}""")}\0";
            Registry.SetValue(keyPath, GENERAL_DATA_h2389025596, Encoding.UTF8.GetBytes(value));
        }
        if (biz.Game is GameBiz.hkrpg)
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



    public static int GetStarRailFPSIndex(GameBiz biz)
    {
        if (biz.Game is GameBiz.hkrpg)
        {
            string key = biz.GetGameRegistryKey();
            byte[]? data = Registry.GetValue(key, GraphicsSettings_Model_h2986158309, null) as byte[];
            if (data is not null)
            {
                string str = Encoding.UTF8.GetString(data).TrimEnd('\0');
                JsonNode? node = JsonNode.Parse(str);
                if (node is not null)
                {
                    int fps = node["FPS"]?.GetValue<int>() ?? 60;
                    return fps switch
                    {
                        30 => 0,
                        120 => 2,
                        _ => 1,
                    };
                }
            }
        }
        return 1;
    }


    public static void SetStarRailFPSIndex(GameBiz biz, int fpsIndex)
    {
        if (biz.Game is GameBiz.hkrpg)
        {
            int fps = fpsIndex switch
            {
                0 => 30,
                1 => 60,
                2 => 120,
                _ => 60,
            };
            string key = biz.GetGameRegistryKey();
            JsonNode? node = null;
            byte[]? data = Registry.GetValue(key, GraphicsSettings_Model_h2986158309, null) as byte[];
            if (data is not null)
            {
                string str = Encoding.UTF8.GetString(data).TrimEnd('\0');
                node = JsonNode.Parse(str);
                node?["FPS"] = fps;
            }

            string? value = node?.ToJsonString();
            if (string.IsNullOrWhiteSpace(value))
            {
                value = $$"""
                    {"FPS":{{fps}},"EnableVSync":false,"RenderScale":1.0,"ResolutionQuality":3,"ShadowQuality":3,"LightQuality":3,"CharacterQuality":3,"EnvDetailQuality":3,"ReflectionQuality":3,"SFXQuality":3,"BloomQuality":3,"AAMode":1,"EnableMetalFXSU":false,"EnableHalfResTransparent":false,"EnableSelfShadow":1,"DlssQuality":0}
                    """;
            }
            value += "\0";
            Registry.SetValue(key, GraphicsSettings_Model_h2986158309, Encoding.UTF8.GetBytes(value));
        }
    }



    public static void SetGenshinEnableHDR(GameBiz biz, bool enableHDR)
    {
        if (biz.Game is GameBiz.hk4e)
        {
            string key = biz.GetGameRegistryKey();
            Registry.SetValue(key, WINDOWS_HDR_ON_h3132281285, enableHDR ? 1 : 0);
        }
    }





    public static (int MaxLuminance, int SceneLuminance, int UILuminance) GetGenshinHDRLuminance(GameBiz biz)
    {
        int max = 1000, scene = 300, ui = 350;
        if (biz.Game is GameBiz.hk4e)
        {
            string key = biz.GetGameRegistryKey();
            byte[]? data = Registry.GetValue(key, GENERAL_DATA_h2389025596, null) as byte[];
            if (data is not null)
            {
                string str = Encoding.UTF8.GetString(data).TrimEnd('\0');
                JsonNode? node = JsonNode.Parse(str);
                if (node is not null)
                {
                    max = (int)(node["maxLuminosity"]?.GetValue<float>() ?? 1000);
                    scene = (int)(node["scenePaperWhite"]?.GetValue<float>() ?? 300);
                    ui = (int)(node["uiPaperWhite"]?.GetValue<float>() ?? 350);
                }
            }
        }
        max = Math.Clamp(max, 300, 2000);
        scene = Math.Clamp(scene, 100, 500);
        ui = Math.Clamp(ui, 150, 550);
        return (max, scene, ui);
    }



    public static void SetGenshinHDRLuminance(GameBiz biz, int maxLuminance, int sceneLuminance, int uiLuminance)
    {
        maxLuminance = Math.Clamp(maxLuminance, 300, 2000);
        sceneLuminance = Math.Clamp(sceneLuminance, 100, 500);
        uiLuminance = Math.Clamp(uiLuminance, 150, 550);
        if (biz.Game is GameBiz.hk4e)
        {
            string key = biz.GetGameRegistryKey();
            byte[]? data = Registry.GetValue(key, GENERAL_DATA_h2389025596, null) as byte[];
            JsonNode? node = null;
            if (data is not null)
            {
                string str = Encoding.UTF8.GetString(data).TrimEnd('\0');
                node = JsonNode.Parse(str);
                node?["maxLuminosity"] = maxLuminance;
                node?["scenePaperWhite"] = sceneLuminance;
                node?["uiPaperWhite"] = uiLuminance;
            }
            string value = $"{node?.ToJsonString() ?? ($$"""{"maxLuminosity":{{maxLuminance}},"scenePaperWhite":{{sceneLuminance}},"uiPaperWhite":{{uiLuminance}}""")}\0";
            Registry.SetValue(key, GENERAL_DATA_h2389025596, Encoding.UTF8.GetBytes(value));
        }
    }










}
