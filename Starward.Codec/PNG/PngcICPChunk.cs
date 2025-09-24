using System.Runtime.InteropServices;

namespace Starward.Codec.PNG;

[StructLayout(LayoutKind.Sequential)]
public struct PngcICPChunk
{
    public byte ColorPrimaries;
    public byte TransferFunction;
    public byte MatrixCoefficients;
    public byte FullRangeFlag;
}
