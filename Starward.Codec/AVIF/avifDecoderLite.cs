using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

public class avifDecoderLite : IDisposable
{

    private unsafe avifDecoder* _decoder;

    private GCHandle _dataHandle;



    internal unsafe avifDecoderLite()
    {
        _decoder = avifNativeMethod.avifDecoderCreate();
        if (_decoder is null)
        {
            throw new avifException("avifDecoderCreate return null.");
        }
        _decoder->maxThreads = Environment.ProcessorCount;
    }



    public static unsafe avifDecoderLite Create(byte[] bytes)
    {
        var decoder = new avifDecoderLite();
        try
        {
            decoder.CreateInternal(bytes);
            return decoder;
        }
        catch
        {
            decoder.Dispose();
            throw;
        }
    }


    public static async Task<avifDecoderLite> CreateAsync(Stream stream, CancellationToken cancellation = default)
    {
        byte[] bytes = new byte[stream.Length];
        await stream.ReadExactlyAsync(bytes, cancellation);
        return Create(bytes);
    }



    internal unsafe void CreateInternal(byte[] bytes)
    {
        _dataHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        avifNativeMethod.avifDecoderSetIOMemory(_decoder, _dataHandle.AddrOfPinnedObject(), (nuint)bytes.Length).ThrowIfFailed("avifDecoderSetIOMemory failed:");
        avifNativeMethod.avifDecoderParse(_decoder).ThrowIfFailed("avifDecoderParse failed:");
    }



    public unsafe uint Width => _decoder->image->width;

    public unsafe uint Height => _decoder->image->height;

    public unsafe uint Depth => _decoder->image->depth;


    public unsafe avifColorPrimaries ColorPrimaries => _decoder->image->colorPrimaries;

    public unsafe avifTransferCharacteristics TransferCharacteristics => _decoder->image->transferCharacteristics;

    public unsafe avifMatrixCoefficients MatrixCoefficients => _decoder->image->matrixCoefficients;


    public unsafe int MaxThreads { get => _decoder->maxThreads; set => _decoder->maxThreads = value; }

    public unsafe bool IgnoreExit { get => _decoder->ignoreExif; set => _decoder->ignoreExif = value; }

    public unsafe bool IgnoreXMP { get => _decoder->ignoreXMP; set => _decoder->ignoreXMP = value; }

    public unsafe uint ImageSizeLimit { get => _decoder->imageSizeLimit; set => _decoder->imageSizeLimit = value; }

    public unsafe uint ImageDimensionLimit { get => _decoder->imageDimensionLimit; set => _decoder->imageDimensionLimit = value; }

    public unsafe uint ImageCountLimit { get => _decoder->imageCountLimit; set => _decoder->imageCountLimit = value; }

    public unsafe avifStrictFlag StrictFlag { get => _decoder->strictFlags; set => _decoder->strictFlags = value; }





    public unsafe int ImageCount => _decoder->imageCount;

    public unsafe avifImageTiming ImageTiming => _decoder->imageTiming;

    public unsafe ulong Timescale => _decoder->timescale;

    public unsafe double Duration => _decoder->duration;

    public unsafe ulong DurationInTimescales => _decoder->durationInTimescales;

    public unsafe int RepetitionCount => _decoder->repetitionCount;

    public unsafe bool AlphaPresent => _decoder->alphaPresent;


    public unsafe ReadOnlySpan<byte> GetIccData()
    {
        if (_decoder->image->icc.Data == 0 || _decoder->image->icc.Size == 0)
        {
            return ReadOnlySpan<byte>.Empty;
        }
        return new ReadOnlySpan<byte>(_decoder->image->icc.Data.ToPointer(), (int)_decoder->image->icc.Size);
    }



    public unsafe avifImageWrapper GetNextImage()
    {
        avifNativeMethod.avifDecoderNextImage(_decoder).ThrowIfFailed("avifDecoderNextImage failed:");
        var wrapper = new avifImageWrapper(_decoder->image);
        wrapper.SuppressDispose(true);
        return wrapper;
    }


    public unsafe avifImageWrapper GetNthImage(uint frameIndex)
    {
        avifNativeMethod.avifDecoderNthImage(_decoder, frameIndex).ThrowIfFailed("avifDecoderNthImage failed:");
        var wrapper = new avifImageWrapper(_decoder->image);
        wrapper.SuppressDispose(true);
        return wrapper;
    }





    private bool disposedValue;


    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // 释放托管状态(托管对象)
            }

            unsafe
            {
                if (_decoder is not null)
                {
                    avifNativeMethod.avifDecoderDestroy(_decoder);
                    _decoder = null;
                }
                if (_dataHandle.IsAllocated)
                {
                    _dataHandle.Free();
                }
            }
            disposedValue = true;
        }
    }

    ~avifDecoderLite()
    {
        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
