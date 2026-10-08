namespace Starward.GameManagement;

public static class GameInstallEnvironment
{

    public static string CacheFolder { get; set; }


    static GameInstallEnvironment()
    {
        CacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward");
    }

}