using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starward.Core;

[Flags]
public enum VoiceLanguage
{

    None = 0,

    [Description("zh-cn")]
    Chinese = 1,

    [Description("en-us")]
    English = 2,

    [Description("ja-jp")]
    Japanese = 4,

    [Description("ko-kr")]
    Korean = 8,

    All = Chinese | English | Japanese | Korean,

}
