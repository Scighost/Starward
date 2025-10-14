using System.Buffers.Binary;

namespace Starward.Codec.PNG;

public class PngReader : IDisposable
{


    private Memory<byte>? data;

    private int position;


    private Stream? stream;

    public static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static readonly byte[] IENDSignature = [0, 0, 0, 0, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82];


    public PngReader(byte[] data)
    {
        this.data = data;
        CheckPngSignature(data);
        position = 8;
    }


    public PngReader(Stream stream)
    {
        this.stream = stream;
        CheckPngSignature(stream);
    }



    private static void CheckPngSignature(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8)
        {
            throw new InvalidDataException("Data is too short to be a valid PNG file.");
        }
        if (!data[..8].SequenceEqual(PngSignature))
        {
            throw new InvalidDataException("Data is not a valid PNG file.");
        }
    }


    private static void CheckPngSignature(Stream stream)
    {
        if (stream.Length < 8)
        {
            throw new InvalidDataException("Data is too short to be a valid PNG file.");
        }
        Span<byte> buffer = stackalloc byte[8];
        stream.ReadExactly(buffer);
        if (!buffer.SequenceEqual(PngSignature))
        {
            throw new InvalidDataException("Data is not a valid PNG file.");
        }
    }




    public PngChunk GetNextChunk()
    {
        if (data.HasValue)
        {
            if (position + 12 > data.Value.Length)
            {
                throw new IndexOutOfRangeException("No more chunks available.");
            }
            Span<byte> span = data.Value.Span;
            uint contentLength = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(position, 4));
            uint chunkLength = contentLength + 12;
            if (position + chunkLength > data.Value.Length)
            {
                throw new IndexOutOfRangeException("No more chunks available.");
            }
            Memory<byte> chunkData = data.Value.Slice(position, (int)chunkLength);
            position += (int)chunkLength;
            return new PngChunk(chunkData);
        }
        if (stream != null)
        {
            if (stream.Position + 12 > stream.Length)
            {
                throw new EndOfStreamException("No more chunks available.");
            }
            Span<byte> header = stackalloc byte[8];
            stream.ReadExactly(header);
            uint contentLength = BinaryPrimitives.ReadUInt32BigEndian(header[..4]);
            if (stream.Position + contentLength + 4 > stream.Length)
            {
                throw new EndOfStreamException("No more chunks available.");
            }
            uint chunkLength = contentLength + 12;
            Memory<byte> chunkData = new byte[chunkLength];
            header.CopyTo(chunkData.Span[..8]);
            stream.ReadExactly(chunkData.Span.Slice(8, (int)contentLength + 4));
            return new PngChunk(chunkData);
        }
        throw new InvalidOperationException("No data source available.");
    }


    public void Dispose()
    {
        stream?.Dispose();
    }

}
