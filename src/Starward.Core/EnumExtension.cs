using Starward.Core.Launcher;
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

}
