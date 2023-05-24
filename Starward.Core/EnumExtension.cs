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
        return biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_global or GameBiz.hk4e_cloud => GameBiz.GenshinImpact,
            GameBiz.hkrpg_cn or GameBiz.hkrpg_global => GameBiz.StarRail,
            _ => GameBiz.None,
        };
    }



}
