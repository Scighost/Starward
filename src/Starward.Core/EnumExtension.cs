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


    public static GameBizEnum ToGame(this GameBizEnum biz)
    {
        return (int)biz switch
        {
            11 or 12 or 13 or 14 => GameBizEnum.GenshinImpact,
            21 or 22 or 24 => GameBizEnum.StarRail,
            >= 31 and <= 37 => GameBizEnum.Honkai3rd,
            41 or 42 or 44 => GameBizEnum.ZZZ,
            _ => GameBizEnum.None,
        };
    }


    public static string ToGameName(this GameBizEnum biz)
    {
        return biz.ToGame() switch
        {
            GameBizEnum.GenshinImpact => CoreLang.Game_GenshinImpact,
            GameBizEnum.StarRail => CoreLang.Game_HonkaiStarRail,
            GameBizEnum.Honkai3rd => CoreLang.Game_HonkaiImpact3rd,
            GameBizEnum.ZZZ => CoreLang.Game_ZZZ,
            _ => "",
        };
    }


    public static string ToGameServer(this GameBizEnum biz)
    {
        return biz switch
        {
            GameBizEnum.hk4e_cn => CoreLang.GameServer_ChinaOfficial,
            GameBizEnum.hk4e_global => CoreLang.GameServer_GlobalOfficial,
            GameBizEnum.hk4e_cloud => CoreLang.GameServer_ChinaCloud,
            GameBizEnum.hk4e_bilibili => CoreLang.GameServer_Bilibili,
            GameBizEnum.hkrpg_cn => CoreLang.GameServer_ChinaOfficial,
            GameBizEnum.hkrpg_global => CoreLang.GameServer_GlobalOfficial,
            GameBizEnum.hkrpg_bilibili => CoreLang.GameServer_Bilibili,
            GameBizEnum.bh3_cn => CoreLang.GameServer_ChinaOfficial,
            GameBizEnum.bh3_global => CoreLang.GameServer_EuropeAmericas,
            GameBizEnum.bh3_jp => CoreLang.GameServer_Japan,
            GameBizEnum.bh3_kr => CoreLang.GameServer_Korea,
            GameBizEnum.bh3_overseas => CoreLang.GameServer_SoutheastAsia,
            GameBizEnum.bh3_tw => CoreLang.GameServer_TraditionalChinese,
            GameBizEnum.nap_cn => CoreLang.GameServer_ChinaOfficial,
            GameBizEnum.nap_global => CoreLang.GameServer_GlobalOfficial,
            GameBizEnum.nap_bilibili => CoreLang.GameServer_Bilibili,
            _ => "",
        };
    }


    public static bool IsChinaServer(this GameBizEnum biz)
    {
        return (int)biz switch
        {
            11 or 13 or 14 or 21 or 24 or 31 or 41 => true,
            _ => false,
        };
    }


    public static bool IsGlobalServer(this GameBizEnum biz)
    {
        return (int)biz switch
        {
            12 or 22 or (>= 32 and <= 36) or 42 => true,
            _ => false,
        };
    }


    public static bool IsBilibiliServer(this GameBizEnum biz)
    {
        return (int)biz switch
        {
            14 or 24 or 44 => true,
            _ => false,
        };
    }



    public static bool IsChinaOfficial(this GameBizEnum biz)
    {
        return biz switch
        {
            GameBizEnum.hk4e_cn or GameBizEnum.hkrpg_cn or GameBizEnum.bh3_cn or GameBizEnum.nap_cn => true,
            _ => false,
        };
    }


    public static bool IsGlobalOfficial(this GameBizEnum biz)
    {
        return biz switch
        {
            GameBizEnum.hk4e_global or GameBizEnum.hkrpg_global or GameBizEnum.bh3_global or GameBizEnum.nap_global => true,
            GameBizEnum.bh3_jp or GameBizEnum.bh3_kr or GameBizEnum.bh3_overseas or GameBizEnum.bh3_tw => true,
            _ => false,
        };
    }


    public static bool IsBilibili(this GameBizEnum biz)
    {
        return biz switch
        {
            GameBizEnum.hk4e_bilibili or GameBizEnum.hkrpg_bilibili or GameBizEnum.nap_bilibili => true,
            _ => false,
        };
    }


    public static bool IsChinaCloud(this GameBizEnum biz)
    {
        return biz switch
        {
            GameBizEnum.hk4e_cloud => true,
            _ => false,
        };
    }




    public static string GetLauncherRegistryKey(this GameBizEnum biz)
    {
        return biz switch
        {
            GameBizEnum.hk4e_cn or GameBizEnum.hk4e_bilibili => GameRegistry.LauncherPath_hk4e_cn,
            GameBizEnum.hk4e_global => GameRegistry.LauncherPath_hk4e_global,
            GameBizEnum.hk4e_cloud => GameRegistry.LauncherPath_hk4e_cloud,
            GameBizEnum.hkrpg_cn or GameBizEnum.hkrpg_bilibili => GameRegistry.LauncherPath_hkrpg_cn,
            GameBizEnum.hkrpg_global => GameRegistry.LauncherPath_hkrpg_global,
            GameBizEnum.bh3_cn => GameRegistry.LauncherPath_bh3_cn,
            GameBizEnum.bh3_global => GameRegistry.LauncherPath_bh3_global,
            GameBizEnum.bh3_jp => GameRegistry.LauncherPath_bh3_jp,
            GameBizEnum.bh3_kr => GameRegistry.LauncherPath_bh3_kr,
            GameBizEnum.bh3_overseas => GameRegistry.LauncherPath_bh3_overseas,
            GameBizEnum.bh3_tw => GameRegistry.LauncherPath_bh3_tw,
            GameBizEnum.nap_cn or GameBizEnum.nap_bilibili => GameRegistry.LauncherPath_HYP_cn,
            GameBizEnum.nap_global => GameRegistry.LauncherPath_HYP_os,
            _ => "HKEY_LOCAL_MACHINE",
        };
    }



    public static string GetGameRegistryKey(this GameBizEnum biz)
    {
        return biz switch
        {
            GameBizEnum.hk4e_cn or GameBizEnum.hk4e_bilibili => GameRegistry.GamePath_hk4e_cn,
            GameBizEnum.hk4e_global => GameRegistry.GamePath_hk4e_global,
            GameBizEnum.hk4e_cloud => GameRegistry.GamePath_hk4e_cloud,
            GameBizEnum.hkrpg_cn or GameBizEnum.hkrpg_bilibili => GameRegistry.GamePath_hkrpg_cn,
            GameBizEnum.hkrpg_global => GameRegistry.GamePath_hkrpg_global,
            GameBizEnum.bh3_cn => GameRegistry.GamePath_bh3_cn,
            GameBizEnum.bh3_global => GameRegistry.GamePath_bh3_global,
            GameBizEnum.bh3_jp => GameRegistry.GamePath_bh3_jp,
            GameBizEnum.bh3_kr => GameRegistry.GamePath_bh3_kr,
            GameBizEnum.bh3_overseas => GameRegistry.GamePath_bh3_overseas,
            GameBizEnum.bh3_tw => GameRegistry.GamePath_bh3_tw,
            GameBizEnum.nap_cn or GameBizEnum.nap_bilibili => GameRegistry.GamePath_nap_cn,
            GameBizEnum.nap_global => GameRegistry.GamePath_nap_global,
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
            GachaType.ChronicledWish => CoreLang.GachaType_ChronicledWish,
            _ => "",
        };
    }


    public static string ToZZZLocalization(this GachaType gachaType)
    {
        return gachaType switch
        {
            GachaType.StandardChannel => CoreLang.GachaType_StandardChannel,
            GachaType.ExclusiveChannel => CoreLang.GachaType_ExclusiveChannel,
            GachaType.WEngineChannel => CoreLang.GachaType_WEngineChannel,
            GachaType.BangbooChannel => CoreLang.GachaType_BangbooChannel,
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



    public static string ToLocalization(this ZZZQueryType zZZQueryType)
    {
        return zZZQueryType switch
        {
            ZZZQueryType.Monochrome => CoreLang.ZZZQueryType_Monochrome,
            ZZZQueryType.Ploychrome => CoreLang.ZZZQueryType_Ploychrome,
            ZZZQueryType.PurchaseGift => CoreLang.ZZZQueryType_Bundle,
            ZZZQueryType.Battery => CoreLang.ZZZQueryType_BatteryCharge,
            ZZZQueryType.Engine => CoreLang.ZZZQueryType_WEngine,
            ZZZQueryType.Disk => CoreLang.ZZZQueryType_DriveDisc,
            _ => "",
        };
    }



}
