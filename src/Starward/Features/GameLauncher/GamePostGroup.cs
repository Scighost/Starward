using Starward.Core.HoYoPlay;
using Starward.Core.Localization;
using System.Collections.Generic;
using System.Linq;

namespace Starward.Features.GameLauncher;

public class GamePostGroup
{

    public string Header { get; set; }

    public List<GamePost> List { get; set; }


    public static List<GamePostGroup> FromGameContent(GameContent content)
    {
        return content.Posts.GroupBy(x => x.Type)
                      .OrderBy(x => x.Key)
                      .Select(x => new GamePostGroup
                      {
                          Header = LocalizePostType(x.Key),
                          List = x.ToList(),
                      }).ToList();
    }



    private static string LocalizePostType(string postType)
    {
        return postType switch
        {
            GamePostType.POST_TYPE_ACTIVITY => CoreLang.PostType_Activity,
            GamePostType.POST_TYPE_ANNOUNCE => CoreLang.PostType_Announcement,
            GamePostType.POST_TYPE_INFO => CoreLang.PostType_Information,
            _ => "",
        };
    }


}
