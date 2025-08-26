using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.CodeStream;

/// <summary>
/// The codestream animation header, optionally present in the beginning of
/// the codestream, and if it is it applies to all animation frames, unlike
/// <see cref="JxlFrameHeader"/> which applies to an individual frame.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlAnimationHeader
{
    /// <summary>
    /// Numerator of ticks per second of a single animation frame time unit
    /// </summary>
    public uint TpsNumerator;

    /// <summary>
    /// Denominator of ticks per second of a single animation frame time unit
    /// </summary>
    public uint TpsDenominator;

    /// <summary>
    /// Amount of animation loops, or 0 to repeat infinitely
    /// </summary>
    public uint NumLoops;

    /// <summary>
    /// Whether animation time codes are present at animation frames in the
    /// codestream
    /// </summary>
    public bool HaveTimeCodes;
}
