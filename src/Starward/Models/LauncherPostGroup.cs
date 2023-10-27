using Starward.Core.Launcher;
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
