namespace Starward.Codec.AVIF;

public enum avifColorPrimaries : ushort
{
    /// <summary>
    /// This is actually reserved, but libavif uses it as a sentinel value.
    /// </summary>
    Unknown = 0,

    BT709 = 1,

    SRGB = 1,

    IEC61966_2_4 = 1,

    Unspecified = 2,

    BT470M = 4,

    BT470BG = 5,

    BT601 = 6,

    SMPTE240 = 7,

    GenericFilm = 8,

    BT2020 = 9,

    BT2100 = 9,

    XYZ = 10,

    SMPTE431 = 11,

    SMPTE432 = 12,

    DCI_P3 = 12,

    EBU3213 = 22

};
