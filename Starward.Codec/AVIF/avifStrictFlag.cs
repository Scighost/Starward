namespace Starward.Codec.AVIF;

/// <summary>
/// Some encoders (including very old versions of avifenc) do not implement the AVIF standard
/// perfectly, and thus create invalid files. However, these files are likely still recoverable /
/// decodable, if it wasn't for the strict requirements imposed by libavif's decoder. These flags
/// allow a user of avifDecoder to decide what level of strictness they want in their project.
/// </summary>
[Flags]
public enum avifStrictFlag : uint
{
    /// <summary>
    /// Disables all strict checks.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Requires the PixelInformationProperty ('pixi') be present in AV1 image items. libheif v1.11.0
    /// or older does not add the 'pixi' item property to AV1 image items. If you need to decode AVIF
    /// images encoded by libheif v1.11.0 or older, be sure to disable this bit. (This issue has been
    /// corrected in libheif v1.12.0.)
    /// </summary>
    PixiRequired = (1 << 0),

    /// <summary>
    /// This demands that the values surfaced in the clap box are valid, determined by attempting to
    /// convert the clap box to a crop rect using avifCropRectFromCleanApertureBox(). If this
    /// function returns AVIF_FALSE and this strict flag is set, the decode will fail.
    /// </summary>
    ClapValid = (1 << 1),

    /// <summary>
    /// Requires the ImageSpatialExtentsProperty ('ispe') be present in alpha auxiliary image items.
    /// avif-serialize 0.7.3 or older does not add the 'ispe' item property to alpha auxiliary image
    /// items. If you need to decode AVIF images encoded by the cavif encoder with avif-serialize
    /// 0.7.3 or older, be sure to disable this bit. (This issue has been corrected in avif-serialize
    /// 0.7.4.) See https://github.com/kornelski/avif-serialize/issues/3 and https://crbug.com/1246678.
    /// </summary>
    AlphaIspeRequired = (1 << 2),

    /// <summary>
    /// Maximum strictness; enables all bits above. This is avifDecoder's default.
    /// </summary>
    Enabled = PixiRequired | ClapValid | AlphaIspeRequired,
}
