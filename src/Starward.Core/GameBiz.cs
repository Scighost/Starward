using System.Collections.ObjectModel;

namespace Starward.Core;

public record GameBiz
{


    public string Value { get; init; }


    public GameBiz(string? value)
    {
        Value = value ?? "";
    }


    public const string bh3 = "bh3";
    public const string bh3_cn = "bh3_cn";
    public const string bh3_global = "bh3_global";

    public const string bh3_os = "bh3_os";      // 东南亚
    public const string bh3_jp = "bh3_jp";
    public const string bh3_kr = "bh3_kr";
    public const string bh3_usa = "bh3_usa";
    public const string bh3_asia = "bh3_asia";  // 繁中
    public const string bh3_eur = "bh3_eur";


    public const string hk4e = "hk4e";
    public const string hk4e_cn = "hk4e_cn";
    public const string hk4e_global = "hk4e_global";
    public const string hk4e_bilibili = "hk4e_bilibili";

    public const string clgm_cn = "clgm_cn";
    public const string clgm_global = "clgm_global";


    public const string hkrpg = "hkrpg";
    public const string hkrpg_cn = "hkrpg_cn";
    public const string hkrpg_global = "hkrpg_global";
    public const string hkrpg_bilibili = "hkrpg_bilibili";


    public const string nap = "nap";
    public const string nap_cn = "nap_cn";
    public const string nap_global = "nap_global";
    public const string nap_bilibili = "nap_bilibili";


    public const string None = "";



    public static ReadOnlyCollection<GameBiz> AllGameBizs { get; private set; } = new List<GameBiz>
    {
        bh3_cn,
        //bh3_global,
        bh3_os,
        bh3_jp,
        bh3_kr,
        bh3_usa,
        bh3_asia,
        bh3_eur,
        hk4e_cn,
        hk4e_global,
        hk4e_bilibili,
        clgm_cn,
        //clgm_global,
        hkrpg_cn,
        hkrpg_global,
        hkrpg_bilibili,
        nap_cn,
        nap_global,
        nap_bilibili,
    }.AsReadOnly();




    public static bool TryParse(string? value, out GameBiz gameBiz)
    {
        gameBiz = new(value);
        return gameBiz.IsKnown();
    }



    public override string ToString() => Value;
    public static implicit operator GameBiz(string? value) => new(value);
    public static implicit operator string(GameBiz value) => value.Value;

}



public static class GameBizExtension
{


    public static bool IsKnown(this GameBiz? gameBiz) => gameBiz?.Value switch
    {
        GameBiz.bh3_cn or GameBiz.bh3_global => true,
        GameBiz.hk4e_cn or GameBiz.hk4e_global or GameBiz.hk4e_bilibili => true,
        GameBiz.clgm_cn or GameBiz.clgm_global => true,
        GameBiz.hkrpg_cn or GameBiz.hkrpg_global or GameBiz.hkrpg_bilibili => true,
        GameBiz.nap_cn or GameBiz.nap_global or GameBiz.nap_bilibili => true,
        _ => false,
    };


    public static bool IsChinaOfficial(this GameBiz? gameBiz) => gameBiz?.Value switch
    {
        GameBiz.bh3_cn => true,
        GameBiz.hk4e_cn => true,
        GameBiz.hkrpg_cn => true,
        GameBiz.nap_cn => true,
        _ => false,
    };


    public static bool IsGlobalOfficial(this GameBiz? gameBiz) => gameBiz?.Value switch
    {
        GameBiz.bh3_global => true,
        GameBiz.hk4e_global => true,
        GameBiz.hkrpg_global => true,
        GameBiz.nap_global => true,
        _ => false,
    };


    public static bool IsBilibili(this GameBiz? gameBiz) => gameBiz?.Value switch
    {
        GameBiz.hk4e_bilibili => true,
        GameBiz.hkrpg_bilibili => true,
        GameBiz.nap_bilibili => true,
        _ => false,
    };


    public static bool IsChinaCloud(this GameBiz? gameBiz) => gameBiz?.Value switch
    {
        GameBiz.clgm_cn => true,
        _ => false,
    };



    public static GameBiz ToGame(this GameBiz? gameBiz) => gameBiz?.Value switch
    {
        GameBiz.bh3_cn or GameBiz.bh3_global => GameBiz.bh3,
        GameBiz.hk4e_cn or GameBiz.hk4e_global or GameBiz.hk4e_bilibili or GameBiz.clgm_cn or GameBiz.clgm_global => GameBiz.hk4e,
        GameBiz.hkrpg_cn or GameBiz.hkrpg_global or GameBiz.hkrpg_bilibili => GameBiz.hkrpg,
        GameBiz.nap_cn or GameBiz.nap_global or GameBiz.nap_bilibili => GameBiz.nap,
        _ => GameBiz.None,
    };


    public static string ToGameName(this GameBiz? gameBiz) => gameBiz?.ToGame().Value switch
    {
        GameBiz.bh3 => CoreLang.Game_HonkaiImpact3rd,
        GameBiz.hk4e => CoreLang.Game_GenshinImpact,
        GameBiz.hkrpg => CoreLang.Game_HonkaiStarRail,
        GameBiz.nap => CoreLang.Game_ZZZ,
        _ => "",
    };


    public static string ToGameServerName(this GameBiz? gameBiz) => gameBiz?.Value switch
    {
        GameBiz.hk4e_cn => CoreLang.GameServer_ChinaOfficial,
        GameBiz.hk4e_global => CoreLang.GameServer_GlobalOfficial,
        GameBiz.clgm_cn => CoreLang.GameServer_ChinaCloud,
        GameBiz.hk4e_bilibili => CoreLang.GameServer_Bilibili,
        GameBiz.hkrpg_cn => CoreLang.GameServer_ChinaOfficial,
        GameBiz.hkrpg_global => CoreLang.GameServer_GlobalOfficial,
        GameBiz.hkrpg_bilibili => CoreLang.GameServer_Bilibili,
        GameBiz.bh3_cn => CoreLang.GameServer_ChinaOfficial,
        GameBiz.bh3_global => CoreLang.GameServer_EuropeAmericas,
        GameBiz.bh3_jp => CoreLang.GameServer_Japan,
        GameBiz.bh3_kr => CoreLang.GameServer_Korea,
        GameBiz.bh3_os => CoreLang.GameServer_SoutheastAsia,
        GameBiz.bh3_asia => CoreLang.GameServer_TraditionalChinese,
        GameBiz.nap_cn => CoreLang.GameServer_ChinaOfficial,
        GameBiz.nap_global => CoreLang.GameServer_GlobalOfficial,
        GameBiz.nap_bilibili => CoreLang.GameServer_Bilibili,
        _ => "",
    };


    public static string GetGameRegistryKey(this GameBiz? gameBiz) => gameBiz?.Value switch
    {
        GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => GameRegistry.GamePath_hk4e_cn,
        GameBiz.hk4e_global => GameRegistry.GamePath_hk4e_global,
        GameBiz.clgm_cn => GameRegistry.GamePath_hk4e_cloud,
        GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => GameRegistry.GamePath_hkrpg_cn,
        GameBiz.hkrpg_global => GameRegistry.GamePath_hkrpg_global,
        GameBiz.bh3_cn => GameRegistry.GamePath_bh3_cn,
        GameBiz.bh3_global => GameRegistry.GamePath_bh3_global,
        GameBiz.bh3_jp => GameRegistry.GamePath_bh3_jp,
        GameBiz.bh3_kr => GameRegistry.GamePath_bh3_kr,
        GameBiz.bh3_os => GameRegistry.GamePath_bh3_overseas,
        GameBiz.bh3_asia => GameRegistry.GamePath_bh3_tw,
        GameBiz.nap_cn or GameBiz.nap_bilibili => GameRegistry.GamePath_nap_cn,
        GameBiz.nap_global => GameRegistry.GamePath_nap_global,
        _ => "HKEY_CURRENT_USER",
    };


}