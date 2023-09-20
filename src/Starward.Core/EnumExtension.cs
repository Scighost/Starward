using Starward.Core.Gacha;
using Starward.Core.Launcher;
using Starward.Core.SelfQuery;
using System.ComponentModel;
using System.Reflection;

namespace Starward.Core;

public static class EnumExtension
{


    public static string ToDescription(this Enum @enum)
    {
        var text = @enum.ToString();
        var attr = @enum.GetType().GetField(text)?.GetCustomAttribute<DescriptionAttribute>();
        if (attr != null)
        {
            return attr.Description;
        }
        else
        {
            return text;
        }
    }


    public static GameBiz ToGame(this GameBiz biz)
    {
        return (int)biz switch
        {
            11 or 12 or 13 => GameBiz.GenshinImpact,
            21 or 22 => GameBiz.StarRail,
            >= 31 and <= 36 => GameBiz.Honkai3rd,
            _ => GameBiz.None,
        };
    }


    public static string ToGameName(this GameBiz biz)
    {
        return biz.ToGame() switch
        {
            GameBiz.GenshinImpact => CoreLang.Game_GenshinImpact,
            GameBiz.StarRail => CoreLang.Game_HonkaiStarRail,
            GameBiz.Honkai3rd => CoreLang.Game_HonkaiImpact3rd,
            _ => "",
        };
    }


    public static string ToGameServer(this GameBiz biz)
    {
        return biz switch
        {
            GameBiz.hk4e_cn => CoreLang.GameServer_ChinaServer,
            GameBiz.hk4e_global => CoreLang.GameServer_GlobalServer,
            GameBiz.hk4e_cloud => CoreLang.GameServer_ChinaCloudServer,
            GameBiz.hkrpg_cn => CoreLang.GameServer_ChinaServer,
            GameBiz.hkrpg_global => CoreLang.GameServer_GlobalServer,
            GameBiz.bh3_cn => CoreLang.GameServer_ChinaServer,
            GameBiz.bh3_global => CoreLang.GameServer_EuropeAmericasServers,
            GameBiz.bh3_jp => CoreLang.GameServer_JapanServer,
            GameBiz.bh3_kr => CoreLang.GameServer_KoreaServer,
            GameBiz.bh3_overseas => CoreLang.GameServer_SEAServer,
            GameBiz.bh3_tw => CoreLang.GameServer_TraditionalChineseServer,
            _ => "",
        };
    }


    public static bool IsChinaServer(this GameBiz biz)
    {
        return (int)biz switch
        {
            11 or 13 or 21 or 31 => true,
            _ => false,
        };
    }


    public static bool IsGlobalServer(this GameBiz biz)
    {
        return (int)biz switch
        {
            12 or 22 or (>= 32 and <= 36) => true,
            _ => false,
        };
    }


    public static string GetLauncherRegistryKey(this GameBiz biz)
    {
        return biz switch
        {
            GameBiz.hk4e_cn => GameRegistry.LauncherPath_hk4e_cn,
            GameBiz.hk4e_global => GameRegistry.LauncherPath_hk4e_global,
            GameBiz.hk4e_cloud => GameRegistry.LauncherPath_hk4e_cloud,
            GameBiz.hkrpg_cn => GameRegistry.LauncherPath_hkrpg_cn,
            GameBiz.hkrpg_global => GameRegistry.LauncherPath_hkrpg_global,
            GameBiz.bh3_cn => GameRegistry.LauncherPath_bh3_cn,
            GameBiz.bh3_global => GameRegistry.LauncherPath_bh3_global,
            GameBiz.bh3_jp => GameRegistry.LauncherPath_bh3_jp,
            GameBiz.bh3_kr => GameRegistry.LauncherPath_bh3_kr,
            GameBiz.bh3_overseas => GameRegistry.LauncherPath_bh3_overseas,
            GameBiz.bh3_tw => GameRegistry.LauncherPath_bh3_tw,
            _ => "HKEY_LOCAL_MACHINE",
        };
    }



    public static string GetGameRegistryKey(this GameBiz biz)
    {
        return biz switch
        {
            GameBiz.hk4e_cn => GameRegistry.GamePath_hk4e_cn,
            GameBiz.hk4e_global => GameRegistry.GamePath_hk4e_global,
            GameBiz.hk4e_cloud => GameRegistry.GamePath_hk4e_cloud,
            GameBiz.hkrpg_cn => GameRegistry.GamePath_hkrpg_cn,
            GameBiz.hkrpg_global => GameRegistry.GamePath_hkrpg_global,
            GameBiz.bh3_cn => GameRegistry.GamePath_bh3_cn,
            GameBiz.bh3_global => GameRegistry.GamePath_bh3_global,
            GameBiz.bh3_jp => GameRegistry.GamePath_bh3_jp,
            GameBiz.bh3_kr => GameRegistry.GamePath_bh3_kr,
            GameBiz.bh3_overseas => GameRegistry.GamePath_bh3_overseas,
            GameBiz.bh3_tw => GameRegistry.GamePath_bh3_tw,
            _ => "HKEY_CURRENT_USER",
        };
    }


    public static string ToLocalization(this GachaType gachaType)
    {
        return gachaType switch
        {
            GachaType.StellarWarp => CoreLang.GachaType_StellarWarp,
            GachaType.DepartureWarp => CoreLang.GachaType_DepartureWarp,
            GachaType.CharacterEventWarp => CoreLang.GachaType_CharacterEventWarp,
            GachaType.LightConeEventWarp => CoreLang.GachaType_LightConeEventWarp,
            GachaType.NoviceWish => CoreLang.GachaType_NoviceWish,
            GachaType.PermanentWish => CoreLang.GachaType_PermanentWish,
            GachaType.CharacterEventWish => CoreLang.GachaType_CharacterEventWish,
            GachaType.CharacterEventWish_2 => CoreLang.GachaType_CharacterEventWish_2,
            GachaType.WeaponEventWish => CoreLang.GachaType_WeaponEventWish,
            _ => "",
        };
    }



    public static string ToLocalization(this PostType postType)
    {
        return postType switch
        {
            PostType.POST_TYPE_ACTIVITY => CoreLang.PostType_Activity,
            PostType.POST_TYPE_ANNOUNCE => CoreLang.PostType_Announcement,
            PostType.POST_TYPE_INFO => CoreLang.PostType_Information,
            _ => "",
        };
    }



    public static string ToLocalization(this GenshinQueryType genshinQueryType)
    {
        return genshinQueryType switch
        {
            GenshinQueryType.Crystal => CoreLang.GenshinQueryType_GenesisCrystal,
            GenshinQueryType.Primogem => CoreLang.GenshinQueryType_Primogem,
            GenshinQueryType.Resin => CoreLang.GenshinQueryType_OriginalResin,
            GenshinQueryType.Artifact => CoreLang.GenshinQueryType_Artifact,
            GenshinQueryType.Weapon => CoreLang.GenshinQueryType_Weapon,
            _ => "",
        };
    }



    public static string ToLocalization(this StarRailQueryType starRailQueryType)
    {
        return starRailQueryType switch
        {
            StarRailQueryType.Stellar => CoreLang.StarRailQueryType_StellarJade,
            StarRailQueryType.Dreams => CoreLang.StarRailQueryType_OneiricShared,
            StarRailQueryType.Relic => CoreLang.StarRailQueryType_Relic,
            StarRailQueryType.Cone => CoreLang.StarRailQueryType_LightCone,
            StarRailQueryType.Power => CoreLang.StarRailQueryType_TrailblazePower,
            _ => "",
        };
    }



}
