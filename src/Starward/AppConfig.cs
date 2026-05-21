using Starward.Features.ViewHost;
using Starward.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Starward;

public partial class AppConfig : Shared.AppConfig
{


    #region Emoji


    public static Uri EmojiPaimon = new Uri("ms-appx:///Assets/Image/UI_EmotionIcon5.png");

    public static Uri EmojiPom = new Uri("ms-appx:///Assets/Image/20008.png");

    public static Uri EmojiAI = new Uri("ms-appx:///Assets/Image/bdfd19c3bdad27a395890755bb60b162.png");

    public static Uri EmojiBangboo = new Uri("ms-appx:///Assets/Image/pamu.db6c2c7b.png");


    #endregion



    static AppConfig()
    {
        ShowWelcomeWindowAsync = async () => await new WelcomeWindow().WaitAsync();
        ShowNoPermissionWindowAsync = async folder => await new NoPermissionWindow(folder).WaitAsync();
    }



    public static new async Task CheckEnviromentAsync()
    {
        await Shared.AppConfig.CheckEnviromentAsync();
        try
        {
            if (Directory.Exists(CacheFolder))
            {
                FileCache.Initialize(Path.Combine(CacheFolder, "cache"));
            }
        }
        catch { }
    }




}
