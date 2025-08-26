using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.CMS;

/// <summary>
/// A point in CIE XY color space.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public record struct JxlPoint
{
    /// <summary>
    /// The X coordinate.
    /// </summary>
    public double X;

    /// <summary>
    /// The Y coordinate.
    /// </summary>
    public double Y;

    /// <summary>
    /// Initializes a new instance of the <see cref="JxlPoint"/> struct.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public JxlPoint(double x, double y)
    {
        X = x;
        Y = y;
    }
}