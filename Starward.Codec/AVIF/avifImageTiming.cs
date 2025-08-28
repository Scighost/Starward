using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

/// <summary>
/// Information about the timing of a single image in an image sequence
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct avifImageTiming
{

    /// <summary>
    /// timescale of the media (Hz)
    /// </summary>
    public ulong Timescale;
    /// <summary>
    /// presentation timestamp in seconds (ptsInTimescales / timescale)
    /// </summary>
    public double Pts;
    /// <summary>
    /// presentation timestamp in "timescales"
    /// </summary>
    public ulong PtsInTimescales;
    /// <summary>
    /// in seconds (durationInTimescales / timescale)
    /// </summary>
    public double Duration;
    /// <summary>
    /// duration in "timescales"
    /// </summary>
    public ulong DurationInTimescales;
}
