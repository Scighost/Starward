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
        var rgbImage = new avifRGBImageWrapper(_image->width, _image->height, depth, format);
        fixed (avifRGBImage* p = &rgbImage._rgbImage)
        {
            avifNativeMethod.avifImageYUVToRGB(_image, p).ThrowIfFailed();
        }
        return rgbImage;
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


    public unsafe void SetExifMetadata(ReadOnlySpan<byte> exif)
    {
        fixed (byte* p = exif)
        {
            avifNativeMethod.avifImageSetMetadataExif(_image, (IntPtr)p, (uint)exif.Length).ThrowIfFailed("Set Exif metadata.");
        }
    }


    public unsafe void SetXMPMetadata(ReadOnlySpan<byte> xmp)
    {
        fixed (byte* p = xmp)
        {
            avifNativeMethod.avifImageSetMetadataXMP(_image, (IntPtr)p, (uint)xmp.Length).ThrowIfFailed("Set XMP metadata.");
        }
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
