namespace Starward.Codec.AVIF;

public class avifImageWrapper : IDisposable
{

    internal unsafe avifImage* _image;


    public unsafe avifImageWrapper(uint width, uint height, uint depth, avifPixelFormat format)
    {
        _image = avifNativeMethod.avifImageCreate(width, height, depth, format);
        if (_image is null)
        {
            throw new avifException("avifImageCreate return null.");
        }
        _image->colorPrimaries = avifColorPrimaries.BT709;
        _image->transferCharacteristics = avifTransferCharacteristics.SRGB;
        _image->matrixCoefficients = avifMatrixCoefficients.BT709;
        _image->yuvRange = avifRange.Full;
    }

    internal unsafe avifImageWrapper(avifImage* image)
    {
        _image = image;
    }


    public unsafe uint Width => _image->width;

    public unsafe uint Height => _image->height;

    public unsafe uint Depth => _image->depth;

    public unsafe avifPixelFormat YUVFormat => _image->yuvFormat;


    public unsafe avifColorPrimaries ColorPrimaries { get => _image->colorPrimaries; set => _image->colorPrimaries = value; }

    public unsafe avifTransferCharacteristics TransferCharacteristics { get => _image->transferCharacteristics; set => _image->transferCharacteristics = value; }

    public unsafe avifMatrixCoefficients MatrixCoefficients { get => _image->matrixCoefficients; set => _image->matrixCoefficients = value; }



    public unsafe void FromRGBImage(avifRGBImageWrapper rgbImage)
    {
        fixed (avifRGBImage* prgb = &rgbImage._rgbImage)
        {
            avifNativeMethod.avifImageRGBToYUV(_image, prgb).ThrowIfFailed("avifImageRGBToYUV failed:");
        }
    }


    public unsafe avifRGBImageWrapper ToRGBImage(uint depth, avifRGBFormat format)
    {
        var rgbImage = new avifRGBImage();
        try
        {
            avifNativeMethod.avifRGBImageSetDefaults(&rgbImage, _image);
            if (depth < rgbImage.depth)
            {
                throw new ArgumentOutOfRangeException(nameof(depth), "Set depth must be greater than or equal to image depth.");
            }
            rgbImage.depth = depth;
            rgbImage.format = format;
            avifNativeMethod.avifRGBImageAllocatePixels(&rgbImage).ThrowIfFailed("avifRGBImageAllocatePixels failed:");
            rgbImage.maxThreads = Environment.ProcessorCount;
            avifNativeMethod.avifImageYUVToRGB(_image, &rgbImage).ThrowIfFailed("avifImageYUVToRGB failed:");
            return new avifRGBImageWrapper(rgbImage);
        }
        catch
        {
            avifNativeMethod.avifRGBImageFreePixels(&rgbImage);
            throw;
        }
    }


    public unsafe void SetMaxCLL(ushort maxCLL, ushort maxPALL)
    {
        _image->clli.MaxCLL = maxCLL;
        _image->clli.MaxPALL = maxPALL;
    }


    public unsafe void SetProfileICC(ReadOnlySpan<byte> icc)
    {
        fixed (byte* p = icc)
        {
            avifNativeMethod.avifImageSetProfileICC(_image, (IntPtr)p, (uint)icc.Length).ThrowIfFailed("Set ICC profile.");
        }
    }


    public unsafe ReadOnlySpan<byte> GetProfileICC()
    {
        if (_image->icc.Data == 0 || _image->icc.Size == 0)
        {
            return ReadOnlySpan<byte>.Empty;
        }
        return new ReadOnlySpan<byte>(_image->icc.Data.ToPointer(), (int)_image->icc.Size);
    }


    public unsafe void SetExifMetadata(ReadOnlySpan<byte> exif)
    {
        fixed (byte* p = exif)
        {
            avifNativeMethod.avifImageSetMetadataExif(_image, (IntPtr)p, (uint)exif.Length).ThrowIfFailed("Set Exif metadata.");
        }
    }


    public unsafe ReadOnlySpan<byte> GetExifMetadata()
    {
        if (_image->exif.Data == 0 || _image->exif.Size == 0)
        {
            return ReadOnlySpan<byte>.Empty;
        }
        return new ReadOnlySpan<byte>(_image->exif.Data.ToPointer(), (int)_image->exif.Size);
    }


    public unsafe void SetXMPMetadata(ReadOnlySpan<byte> xmp)
    {
        fixed (byte* p = xmp)
        {
            avifNativeMethod.avifImageSetMetadataXMP(_image, (IntPtr)p, (uint)xmp.Length).ThrowIfFailed("Set XMP metadata.");
        }
    }


    public unsafe ReadOnlySpan<byte> GetXMPMetadata()
    {
        if (_image->xmp.Data == 0 || _image->xmp.Size == 0)
        {
            return ReadOnlySpan<byte>.Empty;
        }
        return new ReadOnlySpan<byte>(_image->xmp.Data.ToPointer(), (int)_image->xmp.Size);
    }



    internal void SuppressDispose(bool value)
    {
        disposedValue = true;
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

            // 释放未托管的资源(未托管的对象)并重写终结器
            // 将大型字段设置为 null
            unsafe
            {
                if (_image is not null)
                {
                    avifNativeMethod.avifImageDestroy(_image);
                    _image = null;
                }
            }
            disposedValue = true;
        }
    }

    ~avifImageWrapper()
    {
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
