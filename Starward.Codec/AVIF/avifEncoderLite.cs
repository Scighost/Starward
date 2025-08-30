namespace Starward.Codec.AVIF;

public class avifEncoderLite : IDisposable
{

    private unsafe avifEncoder* _encoder;

    private avifRWData _rwData;



    public unsafe avifEncoderLite()
    {
        _encoder = avifNativeMethod.avifEncoderCreate();
        if (_encoder is null)
        {
            throw new avifException("avifEncoderCreate return null.");
        }
        _encoder->maxThreads = Environment.ProcessorCount;
        _encoder->speed = 6;
        _encoder->quality = 90;
        _encoder->qualityAlpha = 90;
        _encoder->autoTiling = true;
    }



    public unsafe int MaxThreads { get => _encoder->maxThreads; set => _encoder->maxThreads = value; }

    public unsafe int Speed { get => _encoder->speed; set => _encoder->speed = value; }

    public unsafe int Quality { get => _encoder->quality; set => _encoder->quality = value; }

    public unsafe int QualityAlpha { get => _encoder->qualityAlpha; set => _encoder->qualityAlpha = value; }


    public unsafe int KeyframeInterval { get => _encoder->keyframeInterval; set => _encoder->keyframeInterval = value; }

    public unsafe ulong Timescale { get => _encoder->timescale; set => _encoder->timescale = value; }

    public unsafe int RepetitionCount { get => _encoder->repetitionCount; set => _encoder->repetitionCount = value; }



    public unsafe void AddImage(avifImageWrapper image, ulong durationInTimescales, avifAddImageFlag addImageFlag)
    {
        avifNativeMethod.avifEncoderAddImage(_encoder, image._image, 1, addImageFlag).ThrowIfFailed("avifEncoderAddImage failed:");
    }


    public unsafe ReadOnlySpan<byte> Encode()
    {
        fixed (avifRWData* prwdata = &_rwData)
        {
            avifNativeMethod.avifEncoderFinish(_encoder, prwdata).ThrowIfFailed("Encode failed:");
        }
        return _rwData.AsSpan();
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
                if (_encoder is not null)
                {
                    avifNativeMethod.avifEncoderDestroy(_encoder);
                    _encoder = null;
                }
                fixed (avifRWData* prwdata = &_rwData)
                {
                    avifNativeMethod.avifRWDataFree(prwdata);
                }
            }
            disposedValue = true;
        }
    }

    ~avifEncoderLite()
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
