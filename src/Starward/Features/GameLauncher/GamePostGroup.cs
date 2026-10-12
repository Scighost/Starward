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
        bool isHypergryphGame = Starward.Core.Hypergryph.HypergryphGameConstants.IsHypergryphGame(content.GameId.GameBiz);
        var groups = content.Posts.GroupBy(x => x.Type);
        var orderedGroups = isHypergryphGame ? groups : groups.OrderBy(x => x.Key);
        return orderedGroups
                      .Select(x => new GamePostGroup
                      {
                          Header = isHypergryphGame ? x.Key : LocalizePostType(x.Key),
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
