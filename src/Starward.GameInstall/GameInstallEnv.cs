namespace Starward.GameInstall;

public static class GameInstallEnv
{

    public static string CacheFolder { get; set; }


    static GameInstallEnv()
    {
        CacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward");
    }

}
