using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

/// <summary>
/// Useful stats related to a read/write
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct avifIOStats
{
    /// <summary>
    /// Size in bytes of the AV1 image item or track data containing color samples.
    /// </summary>
    public nuint ColorOBUSize;

    /// <summary>
    /// Size in bytes of the AV1 image item or track data containing alpha samples.
    /// </summary>
    public nuint AlphaOBUSize;
}



