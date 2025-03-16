using System;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Starward;

internal static class AppConfig
{


    //public static string? AppVersion { get; private set; }


    //public static bool IsPortable { get; private set; }


    //public static IConfigurationRoot Configuration { get; private set; }


    //public static string LogFile { get; private set; }


    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };






    #region UriSource


    public static Uri EmojiPaimon = new Uri("ms-appx:///Assets/Image/UI_EmotionIcon5.png");

    public static Uri EmojiPom = new Uri("ms-appx:///Assets/Image/20008.png");

    public static Uri EmojiAI = new Uri("ms-appx:///Assets/Image/bdfd19c3bdad27a395890755bb60b162.png");

    public static Uri EmojiBangboo = new Uri("ms-appx:///Assets/Image/pamu.db6c2c7b.png");

    #endregion



}
