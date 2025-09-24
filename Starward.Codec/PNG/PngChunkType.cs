using System.Runtime.InteropServices;
using System.Text;

namespace Starward.Codec.PNG;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public record struct PngChunkType
{

    private readonly uint value;


    public PngChunkType(ReadOnlySpan<byte> value)
    {
        if (value.Length > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Length of value must be 4 or less.");
        }
        value.CopyTo(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this, 1)));
    }


    public ReadOnlySpan<byte> AsSpan()
    {
        return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this, 1));
    }


    public override string ToString()
    {
        return Encoding.ASCII.GetString(AsSpan());
    }


    public static implicit operator ReadOnlySpan<byte>(PngChunkType type) => type.AsSpan();

    public static implicit operator PngChunkType(ReadOnlySpan<byte> value) => new(value);


    public static PngChunkType IHDR => "IHDR"u8;
    public static PngChunkType PLTE => "PLTE"u8;
    public static PngChunkType IDAT => "IDAT"u8;
    public static PngChunkType IEND => "IEND"u8;
    public static PngChunkType tRNS => "tRNS"u8;
    public static PngChunkType cHRM => "cHRM"u8;
    public static PngChunkType gAMA => "gAMA"u8;
    public static PngChunkType iCCP => "iCCP"u8;
    public static PngChunkType sBIT => "sBIT"u8;
    public static PngChunkType sRGB => "sRGB"u8;
    public static PngChunkType cICP => "cICP"u8;
    public static PngChunkType mDCV => "cICP"u8;
    public static PngChunkType cLLI => "cLLI"u8;
    public static PngChunkType tEXt => "tEXt"u8;
    public static PngChunkType zTXt => "zTXt"u8;
    public static PngChunkType iTXt => "iTXt"u8;
    public static PngChunkType bKGD => "bKGD"u8;
    public static PngChunkType hIST => "hIST"u8;
    public static PngChunkType pHYs => "pHYs"u8;
    public static PngChunkType sPLT => "sPLT"u8;
    public static PngChunkType eXIf => "eXIf"u8;
    public static PngChunkType tIME => "tIME"u8;
    public static PngChunkType acTL => "acTL"u8;
    public static PngChunkType fcTL => "fcTL"u8;
    public static PngChunkType fdAT => "fdAT"u8;



}
