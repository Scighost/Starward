using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

/// <summary>
/// This struct represents an opaque ItemProperty (Box) or ItemFullProperty (FullBox) in ISO/IEC 14496-12.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct avifImageItemProperty
{
    /// <summary>
    /// boxtype as defined in ISO/IEC 14496-12.
    /// </summary>
    public FixedArray4<byte> boxtype;

    /// <summary>
    /// Universally Unique IDentifier as defined in IETF RFC 4122 and ISO/IEC 9834-8.
    /// Used only when boxtype is "uuid".
    /// </summary>
    public FixedArray16<byte> usertype;

    /// <summary>
    /// BoxPayload as defined in ISO/IEC 14496-12.
    /// Starts with the version (1 byte) and flags (3 bytes) fields in case of a FullBox.
    /// </summary>
    public avifRWData boxPayload;
}
