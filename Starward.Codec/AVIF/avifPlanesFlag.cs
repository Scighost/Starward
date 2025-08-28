namespace Starward.Codec.AVIF;

[Flags]
public enum avifPlanesFlag : uint
{
    YUV = (1 << 0),
    Alpha = (1 << 1),
    ALL = 0xff,
}
