using System.Runtime.InteropServices;

namespace Starward.Codec.PNG;

[StructLayout(LayoutKind.Sequential)]
public struct PnggAMAChunk
{
    private uint _gamma;

    public float Gamma { get => AsSpan().ToUInt32BigEndianFloat(); set => AsSpan().WriteUInt32BigEndianFloat(value); }

    private Span<byte> AsSpan() => MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref _gamma, 1));
}
