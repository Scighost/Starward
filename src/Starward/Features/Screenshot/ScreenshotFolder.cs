namespace Starward.Features.Screenshot;

public class ScreenshotFolder
{

    public string Folder { get; set; }


    public bool InGame { get; set; }

    public bool Backup { get; set; }


    public bool CanRemove => !(InGame || Backup);



    public ScreenshotFolder(string folder)
    {
        Folder = folder;
    }

}