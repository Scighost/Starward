using System.Runtime.InteropServices;

namespace Starward.Codec.PNG;

[StructLayout(LayoutKind.Sequential)]
public struct PngcLLIChunk
{
    private uint _maxCLL;
    private uint _maxFALL;

    public float MaxCLL { get => AsSpan().Slice(0, 4).ToUInt32BigEndian() / 10000f; set => AsSpan().Slice(0, 4).WriteUInt32BigEndian((uint)(value * 10000f)); }

    public float MaxFALL { get => AsSpan().Slice(4, 4).ToUInt32BigEndian() / 10000f; set => AsSpan().Slice(4, 4).WriteUInt32BigEndian((uint)(value * 10000f)); }

    private Span<byte> AsSpan() => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1));
}