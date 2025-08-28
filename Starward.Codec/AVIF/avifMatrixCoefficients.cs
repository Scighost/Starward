namespace Starward.Codec.AVIF;

public enum avifMatrixCoefficients : ushort
{
    Identity = 0,

    BT709 = 1,

    Unspecified = 2,

    FCC = 4,

    BT470BG = 5,

    BT601 = 6,

    SMPTE240 = 7,

    YCGCO = 8,

    BT2020_NCL = 9,

    BT2020_CL = 10,

    SMPTE2085 = 11,

    CHROMA_DERIVED_NCL = 12,

    CHROMA_DERIVED_CL = 13,

    ICTCP = 14,

    YCGCO_RE = 16,

    YCGCO_RO = 17,

    LAST,
};
