namespace Starward.Codec.AVIF;

/// <summary>
/// Optional transformation structs
/// </summary>
[Flags]
public enum avifTransformFlag : uint
{
    NONE = 0,
    PASP = (1 << 0),
    CLAP = (1 << 1),
    IROT = (1 << 2),
    IMIR = (1 << 3),
}
