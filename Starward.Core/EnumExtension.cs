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



}
