using System.ComponentModel;

namespace Starward.Core.Launcher;

public enum PostType
{

    [Description("Activity")]
    POST_TYPE_ACTIVITY,

    [Description("Announcement")]
    POST_TYPE_ANNOUNCE,

    [Description("Information")]
    POST_TYPE_INFO,

}
