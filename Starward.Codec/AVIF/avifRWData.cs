using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

[StructLayout(LayoutKind.Sequential)]
public struct avifRWData
{
    public IntPtr Data;
    public nuint Size;

    public unsafe Span<byte> AsSpan()
    {
        return new Span<byte>((void*)Data, (int)Size);
    }
}
