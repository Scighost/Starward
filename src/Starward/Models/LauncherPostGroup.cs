using Starward.Core.HoYoPlay;
using Starward.Core.Launcher;
using Starward.Core.Localization;
using System.Collections.Generic;
using System.Linq;

namespace Starward.Models;

public class LauncherPostGroup : List<LauncherPost>
{

    public string Header { get; set; }

    public List<LauncherPost> List { get; set; }


    public LauncherPostGroup(string header, IEnumerable<LauncherPost> collection)
    {
        Header = header;
        List = collection.ToList();
    }

}



public class GamePostGroup : List<GamePost>
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


    public static string LocalizePostType(string postType)
    {
        return postType switch
        {
            "POST_TYPE_ACTIVITY" => CoreLang.PostType_Activity,
            "POST_TYPE_ANNOUNCE" => CoreLang.PostType_Announcement,
            "POST_TYPE_INFO" => CoreLang.PostType_Information,
            _ => "",
        };
    }


}