using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Starward.Codec.PNG;

[InlineArray(32)]
public struct PngcHRMChunk
{
    private byte _value0;

    public float WhitePointX { get => AsSpan(0, 4).ToUInt32BigEndianFloat(); set => AsSpan(0, 4).WriteUInt32BigEndianFloat(value); }
    public float WhitePointY { get => AsSpan(4, 4).ToUInt32BigEndianFloat(); set => AsSpan(4, 4).WriteUInt32BigEndianFloat(value); }
    public float RedX { get => AsSpan(8, 4).ToUInt32BigEndianFloat(); set => AsSpan(8, 4).WriteUInt32BigEndianFloat(value); }
    public float RedY { get => AsSpan(12, 4).ToUInt32BigEndianFloat(); set => AsSpan(12, 4).WriteUInt32BigEndianFloat(value); }
    public float GreenX { get => AsSpan(16, 4).ToUInt32BigEndianFloat(); set => AsSpan(16, 4).WriteUInt32BigEndianFloat(value); }
    public float GreenY { get => AsSpan(20, 4).ToUInt32BigEndianFloat(); set => AsSpan(20, 4).WriteUInt32BigEndianFloat(value); }
    public float BlueX { get => AsSpan(24, 4).ToUInt32BigEndianFloat(); set => AsSpan(24, 4).WriteUInt32BigEndianFloat(value); }
    public float BlueY { get => AsSpan(28, 4).ToUInt32BigEndianFloat(); set => AsSpan(28, 4).WriteUInt32BigEndianFloat(value); }

    public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref _value0, 32);

    public Span<byte> AsSpan(int start, int length) => AsSpan().Slice(start, length);

}
