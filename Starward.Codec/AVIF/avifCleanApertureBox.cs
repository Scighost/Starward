using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;



/// <summary>
/// 'clap' from ISO/IEC 14496-12:2022 12.1.4.3
/// Note that ISO/IEC 23000-22:2024 7.3.6.7 requires the decoded image to be upsampled to 4:4:4 before
/// clean aperture is applied if a clean aperture size or offset is odd in a subsampled dimension.
/// However, AV1 supports odd dimensions with chroma subsampling in those directions, so only apply the
/// requirements to offsets.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct avifCleanApertureBox
{


    /// <summary>
    /// a fractional number which defines the width of the clean aperture image
    /// </summary>
    public uint WidthN;
    /// <summary>
    /// a fractional number which defines the width of the clean aperture image
    /// </summary>
    public uint WidthD;

    /// <summary>
    /// a fractional number which defines the height of the clean aperture image
    /// </summary>
    public uint HeightN;
    /// <summary>
    /// a fractional number which defines the height of the clean aperture image
    /// </summary>
    public uint HeightD;

    /// <summary>
    /// a fractional number which defines the horizontal offset between the clean aperture image
    /// centre and the full aperture image centre. Typically 0.
    /// </summary>
    public uint HorizOffN;
    /// <summary>
    /// a fractional number which defines the horizontal offset between the clean aperture image
    /// centre and the full aperture image centre. Typically 0.
    /// </summary>
    public uint HorizOffD;

    /// <summary>
    /// a fractional number which defines the vertical offset between clean aperture image centre
    /// and the full aperture image centre. Typically 0.
    /// </summary>
    public uint VertOffN;
    /// <summary>
    /// a fractional number which defines the vertical offset between clean aperture image centre
    /// and the full aperture image centre. Typically 0.
    /// </summary>
    public uint VertOffD;
}
