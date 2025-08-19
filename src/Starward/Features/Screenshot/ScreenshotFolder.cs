namespace Starward.Features.Screenshot;

public class ScreenshotFolder
{

    public string Folder { get; set; }

    public bool Default { get; set; }

    public bool InGame { get; set; }

    public bool CanRemove => !(InGame || Default);


    public ScreenshotFolder(string folder)
    {
        Folder = folder;
    }

}