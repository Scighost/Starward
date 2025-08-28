using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

public class avifRGBImageWrapper : IDisposable
{

    internal avifRGBImage _rgbImage;

    private GCHandle _dataHandle;


    public avifRGBImageWrapper(uint width, uint height, uint depth, avifRGBFormat format)
    {
        _rgbImage = new avifRGBImage();
        _rgbImage.width = width;
        _rgbImage.height = height;
        _rgbImage.depth = depth;
        _rgbImage.format = format;
        _rgbImage.maxThreads = Environment.ProcessorCount;
        _rgbImage.rowBytes = width * (depth > 8 ? 2u : 1u) * avifNativeMethod.avifRGBFormatChannelCount(format);
    }



    public uint Width => _rgbImage.width;

    public uint Height => _rgbImage.height;

    public uint Depth => _rgbImage.depth;

    public avifRGBFormat Format => _rgbImage.format;

    public int MaxThreads { get => _rgbImage.maxThreads; set => _rgbImage.maxThreads = value; }

    public bool AlplhaPremultiplied { get => _rgbImage.alphaPremultiplied; set => _rgbImage.alphaPremultiplied = value; }

    public avifChromaUpsampling ChromaUpsampling { get => _rgbImage.chromaUpsampling; set => _rgbImage.chromaUpsampling = value; }

    public avifChromaDownsampling ChromaDownsampling { get => _rgbImage.chromaDownsampling; set => _rgbImage.chromaDownsampling = value; }

    public bool AvoidLibYUV { get => _rgbImage.avoidLibYUV; set => _rgbImage.avoidLibYUV = value; }

    public bool IgnoreAlpha { get => _rgbImage.ignoreAlpha; set => _rgbImage.ignoreAlpha = value; }

    public bool IsFloat { get => _rgbImage.isFloat; set => _rgbImage.isFloat = value; }



    public void SetPixelBytes(byte[] bytes)
    {
        if (_dataHandle.IsAllocated)
        {
            _dataHandle.Free();
        }
        _dataHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        _rgbImage.pixels = _dataHandle.AddrOfPinnedObject();
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
            if (_dataHandle.IsAllocated)
            {
                _dataHandle.Free();
            }
            disposedValue = true;
        }
    }

    ~avifRGBImageWrapper()
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
