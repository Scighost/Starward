using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

[StructLayout(LayoutKind.Sequential)]
public record struct avifROData
{
    public readonly IntPtr Data;
    public nuint Size;


    public unsafe ReadOnlySpan<byte> AsSpan() => new ReadOnlySpan<byte>((void*)Data, (int)Size);

}
