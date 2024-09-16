namespace Starward.Core.SelfQuery;

public static class SelfQueryTpyeEnumExtension
{

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