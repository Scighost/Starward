using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.CMS;

/// <summary>
/// Color encoding of the image as structured information.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlColorEncoding : IEquatable<JxlColorEncoding>
{
    /// <summary>
    /// Color space of the image data.
    /// </summary>
    public JxlColorSpace ColorSpace;

    /// <summary>
    /// Built-in white point. If this value is <see cref="JxlWhitePoint.Custom"/>, must
    /// use the numerical white point values from <see cref="WhitePointXY"/>.
    /// </summary>
    public JxlWhitePoint WhitePoint;

    /// <summary>
    /// Numerical whitepoint values in CIE xy space.
    /// </summary>
    public JxlPoint WhitePointXY;

    /// <summary>
    /// Built-in RGB primaries. If this value is <see cref="JxlPrimaries.Custom"/>, must
    /// use the numerical primaries values below. This field and the custom values
    /// below are unused and must be ignored if the color space is
    /// <see cref="JxlColorSpace.Gray"/> or <see cref="JxlColorSpace.XYB"/>.
    /// </summary>
    public JxlPrimaries Primaries;

    /// <summary>
    /// Numerical red primary values in CIE xy space.
    /// </summary>
    public JxlPoint PrimariesRedXY;

    /// <summary>
    /// Numerical green primary values in CIE xy space.
    /// </summary>
    public JxlPoint PrimariesGreenXY;

    /// <summary>
    /// Numerical blue primary values in CIE xy space.
    /// </summary>
    public JxlPoint PrimariesBlueXY;

    /// <summary>
    /// Transfer function if <c>have_gamma</c> is 0
    /// </summary>
    public JxlTransferFunction TransferFunction;

    /// <summary>
    /// Gamma value used when <see cref="TransferFunction"/> is <see cref="JxlTransferFunction.Gamma"/>
    /// </summary>
    public double Gamma;

    /// <summary>
    /// Rendering intent defined for the color profile.
    /// </summary>
    public JxlRenderingIntent RenderingIntent;



    /// <summary>
    /// sRGB color encoding
    /// </summary>
    public static JxlColorEncoding SRGB => new JxlColorEncoding
    {
        ColorSpace = JxlColorSpace.RGB,
        Primaries = JxlPrimaries.sRGB,
        WhitePoint = JxlWhitePoint.D65,
        TransferFunction = JxlTransferFunction.sRGB,
    };


    /// <summary>
    /// Linear sRGB color encoding
    /// </summary>
    public static JxlColorEncoding LinearSRGB => new JxlColorEncoding
    {
        ColorSpace = JxlColorSpace.RGB,
        WhitePoint = JxlWhitePoint.D65,
        Primaries = JxlPrimaries.sRGB,
        TransferFunction = JxlTransferFunction.Linear,
    };


    /// <summary>
    /// Display P3 color encoding
    /// </summary>
    public static JxlColorEncoding DisplayP3 => new JxlColorEncoding
    {
        ColorSpace = JxlColorSpace.RGB,
        WhitePoint = JxlWhitePoint.D65,
        Primaries = JxlPrimaries.P3,
        TransferFunction = JxlTransferFunction.sRGB,
    };


    /// <summary>
    /// HDR10 color encoding
    /// </summary>
    public static JxlColorEncoding HDR10 => new JxlColorEncoding
    {
        ColorSpace = JxlColorSpace.RGB,
        WhitePoint = JxlWhitePoint.D65,
        Primaries = JxlPrimaries.BT2100,
        TransferFunction = JxlTransferFunction.PQ,
    };


    public bool Equals(JxlColorEncoding other)
    {
        if (ColorSpace != other.ColorSpace)
            return false;

        if (WhitePoint != JxlWhitePoint.Custom && other.WhitePoint != JxlWhitePoint.Custom && WhitePoint != other.WhitePoint)
            return false;

        if (Primaries != JxlPrimaries.Custom && other.Primaries != JxlPrimaries.Custom && Primaries != other.Primaries)
            return false;

        // Compare TransferFunction - if both are not Custom/Gamma and different, return false
        if (TransferFunction != JxlTransferFunction.Gamma && other.TransferFunction != JxlTransferFunction.Gamma && TransferFunction != other.TransferFunction)
            return false;

        if (ColorSpace == other.ColorSpace && WhitePoint == other.WhitePoint && Primaries == other.Primaries && TransferFunction == other.TransferFunction)
        {
            return true;
        }

        return WhitePointXY.Equals(other.WhitePointXY) &&
               PrimariesRedXY.Equals(other.PrimariesRedXY) &&
               PrimariesGreenXY.Equals(other.PrimariesGreenXY) &&
               PrimariesBlueXY.Equals(other.PrimariesBlueXY) &&
               Gamma.Equals(other.Gamma) &&
               RenderingIntent == other.RenderingIntent;
    }


    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is JxlColorEncoding encoding && this.Equals(encoding);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }


    public static bool operator ==(JxlColorEncoding left, JxlColorEncoding right) => left.Equals(right);

    public static bool operator !=(JxlColorEncoding left, JxlColorEncoding right) => !(left == right);


}
