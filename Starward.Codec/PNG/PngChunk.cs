using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Starward.Codec.PNG;

public class PngChunk
{

    public int ChunkLength { get; init; }


    public PngChunkType Type { get; init; }


    public uint CRC32 { get; private set; }


    public Memory<byte> ChunkData { get; init; }



    public PngChunk(Memory<byte> chunkData)
    {
        if (chunkData.Length < 12)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkData), "Chunk data must be at least 12 bytes long.");
        }
        ChunkData = chunkData;
        ChunkLength = chunkData.Length;
        Type = chunkData.Span[4..8];
        CRC32 = BinaryPrimitives.ReadUInt32BigEndian(chunkData.Span[^4..]);
    }


    public PngChunk(int contentLength, PngChunkType type)
    {
        ChunkLength = contentLength + 12;
        Type = type;
        ChunkData = new byte[ChunkLength];
        ChunkData.Span[..4].WriteUInt32BigEndian((uint)contentLength);
        Type.AsSpan().CopyTo(ChunkData.Span[4..8]);
        UpdateCrc32();
    }


    public PngChunk(PngChunkType type, ReadOnlySpan<byte> content)
    {
        int contentLength = content.Length;
        ChunkLength = contentLength + 12;
        Type = type;
        ChunkData = new byte[ChunkLength];
        ChunkData.Span[..4].WriteUInt32BigEndian((uint)contentLength);
        Type.AsSpan().CopyTo(ChunkData.Span[4..8]);
        content.CopyTo(ChunkData.Span[8..^4]);
        UpdateCrc32();
    }



    public int ContentLength => ChunkLength - 12;


    public Span<byte> ContentData => ChunkData.Span[8..^4];



    private ref T ContentAsRef<T>() where T : struct
    {
        if (ContentLength < Unsafe.SizeOf<T>())
        {
            throw new InvalidOperationException($"Content data is too short to be interpreted as {typeof(T).Name}.");
        }
        return ref MemoryMarshal.AsRef<T>(ContentData);
    }


    public ref PngcHRMChunk GetcHRMChunk()
    {
        if (Type != PngChunkType.cHRM)
        {
            throw new InvalidOperationException("Chunk is not a cHRM chunk.");
        }
        return ref ContentAsRef<PngcHRMChunk>();
    }


    public ref PnggAMAChunk GetgAMAChunk()
    {
        if (Type != PngChunkType.gAMA)
        {
            throw new InvalidOperationException("Chunk is not a gAMA chunk.");
        }
        return ref ContentAsRef<PnggAMAChunk>();
    }


    public ref PngsRGBChunk GetsRGBChunk()
    {
        if (Type != PngChunkType.sRGB)
        {
            throw new InvalidOperationException("Chunk is not a sRGB chunk.");
        }
        return ref ContentAsRef<PngsRGBChunk>();
    }


    public ref PngcICPChunk GetcICPChunk()
    {
        if (Type != PngChunkType.cICP)
        {
            throw new InvalidOperationException("Chunk is not a cICP chunk.");
        }
        return ref ContentAsRef<PngcICPChunk>();
    }


    public ref PngcLLIChunk GetcLLIChunk()
    {
        if (Type != PngChunkType.cLLI)
        {
            throw new InvalidOperationException("Chunk is not a cLLI chunk.");
        }
        return ref ContentAsRef<PngcLLIChunk>();
    }


    public byte[] GetiCCPChunk(out string profileName)
    {
        if (Type != PngChunkType.iCCP)
        {
            throw new InvalidOperationException("Chunk is not a iCCP chunk.");
        }
        Span<byte> content = ContentData;
        int nullIndex = content.IndexOf((byte)0);
        if (nullIndex + 7 > content.Length)
        {
            throw new IndexOutOfRangeException("iCCP chunk content is too short.");
        }
        profileName = Encoding.ASCII.GetString(content[..nullIndex]);
        Span<byte> compressedData = content[(nullIndex + 2)..];
        using var ms = new MemoryStream(compressedData.ToArray());
        using var zlib = new ZLibStream(ms, CompressionMode.Decompress);
        using var result = new MemoryStream();
        zlib.CopyTo(result);
        return result.ToArray();
    }



    public bool CheckCrc32()
    {
        uint crc = 0xFFFFFFFF;
        crc = PngChunkHelper.Crc32(crc, ChunkData.Span[4..^4]);
        crc ^= 0xFFFFFFFF;
        return crc == CRC32;
    }


    public void UpdateCrc32()
    {
        uint crc = 0xFFFFFFFF;
        crc = PngChunkHelper.Crc32(crc, ChunkData.Span[4..^4]);
        crc ^= 0xFFFFFFFF;
        CRC32 = crc;
        BinaryPrimitives.WriteUInt32BigEndian(ChunkData.Span[^4..], CRC32);
    }


}
