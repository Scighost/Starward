using Microsoft.Win32;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Starward;

public static partial class AppConfig
{

    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static string StarwardExecutePath => Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "Starward.exe");
    public static Guid DeviceId { get; private set; }



    static AppConfig()
    {
        LoadConfiguration();
        GetDeviceId();
    }



    private static void GetDeviceId()
    {
        string? systemBiosVersion = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System", "SystemBiosVersion", null) as string;
        string? machineGuid = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography", "MachineGuid", null) as string;
        DeviceId = new(MD5.HashData(Encoding.UTF8.GetBytes($"{systemBiosVersion}{machineGuid}{Environment.MachineName}")));
    }



    #region Emoji


    public static Uri EmojiPaimon = new Uri("ms-appx:///Assets/Image/UI_EmotionIcon5.png");

    public static Uri EmojiPom = new Uri("ms-appx:///Assets/Image/20008.png");

    public static Uri EmojiAI = new Uri("ms-appx:///Assets/Image/bdfd19c3bdad27a395890755bb60b162.png");

    public static Uri EmojiBangboo = new Uri("ms-appx:///Assets/Image/pamu.db6c2c7b.png");


    #endregion



}
