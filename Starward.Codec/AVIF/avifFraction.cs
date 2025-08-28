using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

[StructLayout(LayoutKind.Sequential)]
public record struct avifFraction
{
    public int Numerator;

    public int Denominator;
}


[StructLayout(LayoutKind.Sequential)]
public struct avifSignedFraction
{
    public int Numerator;

    public uint Denominator;
}


[StructLayout(LayoutKind.Sequential)]
public struct avifUnsignedFraction
{
    public uint Numerator;

    public uint Denominator;
}