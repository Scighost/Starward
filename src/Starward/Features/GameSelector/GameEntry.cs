namespace Starward.Features.GameSelector;

public class GameEntry
{
    public string GameId { get; set; }

    public string GameBiz { get; set; }

    public string LauncherId { get; set; }

    public string? InstallPath { get; set; }

    public bool Removable { get; set; }

    public bool IsPinned { get; set; }

    public int PinnedOrder { get; set; }
}