namespace Starward.Features.Background;

internal class VideoBgVolumeChangedMessage
{

    public int Volume { get; set; }

    public VideoBgVolumeChangedMessage(int volume)
    {
        Volume = volume;
    }

}
