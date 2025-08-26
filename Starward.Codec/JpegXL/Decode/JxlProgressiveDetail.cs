namespace Starward.Codec.JpegXL.Decode;

/// <summary>
/// <para>
/// Types of progressive detail.
/// Setting a progressive detail with value N implies all progressive details
/// with smaller or equal value. Currently only the following level of
/// progressive detail is implemented:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="kDC"/> (which implies <see cref="kFrames"/>)</description></item>
/// <item><description><see cref="kLastPasses"/> (which implies <see cref="kDC"/> and <see cref="kFrames"/>)</description></item>
/// <item><description><see cref="kPasses"/> (which implies <see cref="kLastPasses"/>, <see cref="kDC"/> and <see cref="kFrames"/>)</description></item>
/// </list>
/// </summary>
public enum JxlProgressiveDetail
{
    /// <summary>
    /// after completed kRegularFrames
    /// </summary>
    kFrames = 0,

    /// <summary>
    /// after completed DC (1:8)
    /// </summary>
    kDC = 1,

    /// <summary>
    /// after completed AC passes that are the last pass for their resolution target.
    /// </summary>
    kLastPasses = 2,

    /// <summary>
    /// after completed AC passes that are not the last pass for their resolution target.
    /// </summary>
    kPasses = 3,

    /// <summary>
    /// during DC frame when lower resolution are completed (1:32, 1:16)
    /// </summary>
    kDCProgressive = 4,

    /// <summary>
    /// after completed groups
    /// </summary>
    kDCGroups = 5,

    /// <summary>
    /// after completed groups
    /// </summary>
    kGroups = 6,
}
