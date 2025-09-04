using Starward.Codec.JpegXL.CMS;
using Starward.Codec.JpegXL.CodeStream;
using Starward.Codec.JpegXL.ParallelRunner;
using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.Decode;

/// <summary>
/// A lightweight JPEG XL decoder.
/// </summary>
public class JxlDecoderLite : IDisposable
{
    /// <summary>
    /// Gets the version of the underlying JxlDecoder library.
    /// </summary>
    public static Version Version => GetDecoderVersion();

    /// <summary>
    /// Gets the decoder library version.
    /// </summary>
    /// <returns>The decoder library version.</returns>
    private static Version GetDecoderVersion()
    {
        uint version = JxlDecoderNativeMethod.JxlDecoderVersion();
        uint patch = version % 1000;
        uint minor = (version / 1000) % 1000;
        uint major = version / 1000000;
        return new Version((int)major, (int)minor, (int)patch);
    }

    /// <summary>
    /// Checks the signature of a JPEG XL file.
    /// </summary>
    /// <param name="buffer">The buffer containing the file data.</param>
    /// <returns>The signature check result.</returns>
    public static unsafe JxlSignature CheckSignature(ReadOnlySpan<byte> buffer)
    {
        fixed (byte* p = buffer)
        {
            return JxlDecoderNativeMethod.JxlSignatureCheck((nint)p, (nuint)buffer.Length);
        }
    }

    /// <summary>
    /// Checks the signature of a JPEG XL file from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the file data.</param>
    /// <returns>The signature check result.</returns>
    public static JxlSignature CheckSignature(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[12];
        int bytesRead = stream.Read(buffer);
        return CheckSignature(buffer.Slice(0, bytesRead));
    }



    /// <summary>
    /// Pointer to the JxlDecoder instance.
    /// </summary>
    private JxlDecoderPtr _decoderPtr;

    /// <summary>
    /// Pointer to the resizable parallel runner.
    /// </summary>
    private JxlResizableParallelRunnerPtr _parallelRunnerPtr;

    /// <summary>
    /// Pointer to the JxlResizableParallelRunner function.
    /// </summary>
    private IntPtr _jxlParallelRunnerFunction;

    /// <summary>
    /// The color management system interface.
    /// </summary>
    private JxlCmsInterface _cmsInterface;

    /// <summary>
    /// Initializes a new instance of the <see cref="JxlDecoderLite"/> class.
    /// </summary>
    /// <exception cref="JxlDecodeException">Thrown when the decoder cannot be created.</exception>
    internal JxlDecoderLite()
    {
        _decoderPtr = JxlDecoderNativeMethod.JxlDecoderCreate();
        if (_decoderPtr == IntPtr.Zero)
        {
            throw new JxlDecodeException("Failed to create JxlDecoder.");
        }
        _parallelRunnerPtr = JxlResizableParallelRunnerPtr.GetDefault();
        _jxlParallelRunnerFunction = JxlParallelRunnerNativeMethod.GetJxlResizableParallelRunner();
        _cmsInterface = JxlCmsInterface.GetDefault();
        JxlDecoderNativeMethod.JxlDecoderSetParallelRunner(_decoderPtr, _jxlParallelRunnerFunction, _parallelRunnerPtr);
        JxlDecoderNativeMethod.JxlDecoderSetCms(_decoderPtr, _cmsInterface);
    }



    /// <summary>
    /// Creates a new instance of the <see cref="JxlDecoderLite"/> class from a byte array.
    /// </summary>
    /// <param name="bytes">The input byte array.</param>
    /// <returns>A new instance of the <see cref="JxlDecoderLite"/> class.</returns>
    public static JxlDecoderLite Create(byte[] bytes)
    {
        var decoder = new JxlDecoderLite();
        try
        {
            decoder.Initialize(bytes);
            return decoder;
        }
        catch
        {
            decoder.Dispose();
            throw;
        }
    }


    /// <summary>
    /// Creates a new instance of the <see cref="JxlDecoderLite"/> class from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new instance of the <see cref="JxlDecoderLite"/> class.</returns>
    public static async Task<JxlDecoderLite> CreateAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        byte[] bytes = new byte[stream.Length];
        await stream.ReadExactlyAsync(bytes, cancellationToken);
        return Create(bytes);
    }




    private GCHandle _pinnedBytes;


    private int _inputBytesLength;


    private JxlBasicInfo _basicInfo;


    private JxlPixelFormat _pixelFormat;


    private JxlColorEncoding _colorEncoding;

    /// <summary>
    /// Gets the width of the image.
    /// </summary>
    public uint Width => _basicInfo.XSize;

    /// <summary>
    /// Gets the height of the image.
    /// </summary>
    public uint Height => _basicInfo.YSize;

    /// <summary>
    /// Gets the color encoding of the image.
    /// </summary>
    public JxlColorEncoding ColorEncoding => _colorEncoding;

    /// <summary>
    /// Gets the pixel format of the image.
    /// </summary>
    public JxlPixelFormat PixelFormat => _pixelFormat;


    private byte[]? _exifData;

    private GCHandle _pinnedExifData;

    private byte[]? _xmpData;

    private GCHandle _pinnedXmpData;


    /// <summary>
    /// The buffer for the decoded pixels.
    /// </summary>
    private byte[]? _pixelBuffer;

    /// <summary>
    /// GCHandle for the pinned pixel buffer.
    /// </summary>
    private GCHandle _pinnedPixelBuffer;


    /// <summary>
    /// Initializes the decoder with the input byte array.
    /// </summary>
    /// <param name="bytes">The input byte array.</param>
    /// <exception cref="JxlDecodeException">Thrown when initialization fails.</exception>
    internal void Initialize(byte[] bytes)
    {
        JxlSignature signature = CheckSignature(bytes);
        if (signature is JxlSignature.NotEnoughBytes or JxlSignature.Invalid)
        {
            throw new JxlDecodeException("Invalid JPEG XL signature.");
        }
        JxlDecoderStatus status = JxlDecoderNativeMethod.JxlDecoderSubscribeEvents(_decoderPtr, JxlDecoderStatus.BasicInfo | JxlDecoderStatus.ColorEncoding | JxlDecoderStatus.Box | JxlDecoderStatus.BoxComplete);
        if (status != JxlDecoderStatus.Success)
        {
            throw new JxlDecodeException($"Failed to subscribe events.");
        }
        status = JxlDecoderNativeMethod.JxlDecoderSetDecompressBoxes(_decoderPtr, true);
        if (status != JxlDecoderStatus.Success)
        {
            throw new JxlDecodeException($"Failed to set decompress boxes.");
        }
        _pinnedBytes = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        _inputBytesLength = bytes.Length;
        status = JxlDecoderNativeMethod.JxlDecoderSetInput(_decoderPtr, _pinnedBytes.AddrOfPinnedObject(), (nuint)bytes.Length);
        if (status != JxlDecoderStatus.Success)
        {
            throw new JxlDecodeException($"Failed to set input.");
        }
        JxlDecoderNativeMethod.JxlDecoderCloseInput(_decoderPtr);
        ProcessInfo();
    }


    /// <summary>
    /// Processes the input to get basic info and color encoding.
    /// </summary>
    /// <exception cref="JxlDecodeException">Thrown when processing fails.</exception>
    /// <exception cref="NotSupportedException">Thrown when the pixel format is not supported.</exception>
    private void ProcessInfo()
    {
        JxlBoxType boxType = new();
        while (true)
        {
            JxlDecoderStatus status = JxlDecoderNativeMethod.JxlDecoderProcessInput(_decoderPtr);
            if (status is JxlDecoderStatus.BasicInfo)
            {
                status = JxlDecoderNativeMethod.JxlDecoderGetBasicInfo(_decoderPtr, ref _basicInfo);
                if (status != JxlDecoderStatus.Success)
                {
                    throw new JxlDecodeException($"Failed to get basic info.");
                }
                nuint threads = JxlParallelRunnerNativeMethod.JxlResizableParallelRunnerSuggestThreads(_basicInfo.XSize, _basicInfo.YSize);
                JxlParallelRunnerNativeMethod.JxlResizableParallelRunnerSetThreads(_parallelRunnerPtr, threads);
                _pixelFormat = new JxlPixelFormat
                {
                    NumChannels = _basicInfo.NumColorChannels + (_basicInfo.AlphaBits > 0 ? 1u : 0),
                    DataType = (_basicInfo.BitsPerSample, _basicInfo.ExponentBitsPerSample) switch
                    {
                        (8, 0) => JxlDataType.UInt8,
                        ( > 8, 0) => JxlDataType.UInt16,
                        (16, 5) => JxlDataType.Float16,
                        (32, 8) => JxlDataType.Float,
                        _ => throw new JxlDecodeException($"Unsupported bits per sample: {_basicInfo.BitsPerSample}, exponent bits per sample: {_basicInfo.ExponentBitsPerSample}"),
                    },
                    Endianness = JxlEndianness.Native,
                    Align = 0
                };
            }
            else if (status is JxlDecoderStatus.ColorEncoding)
            {
                status = JxlDecoderNativeMethod.JxlDecoderGetColorAsEncodedProfile(_decoderPtr, JxlColorProfileTarget.Data, ref _colorEncoding);
            }
            else if (status is JxlDecoderStatus.Box)
            {
                status = JxlDecoderNativeMethod.JxlDecoderGetBoxType(_decoderPtr, ref boxType, true);
                if (status != JxlDecoderStatus.Success)
                {
                    throw new JxlDecodeException("Failed to get box type.");
                }
                JxlDecoderNativeMethod.JxlDecoderReleaseBoxBuffer(_decoderPtr);
                if (boxType == JxlBoxType.Exif)
                {
                    ulong size = 0;
                    status = JxlDecoderNativeMethod.JxlDecoderGetBoxSizeContents(_decoderPtr, ref size);
                    if (status != JxlDecoderStatus.Success)
                    {
                        throw new JxlDecodeException("Failed to get Exif box size.");
                    }
                    _exifData = new byte[size];
                    _pinnedExifData = GCHandle.Alloc(_exifData, GCHandleType.Pinned);
                    status = JxlDecoderNativeMethod.JxlDecoderSetBoxBuffer(_decoderPtr, _pinnedExifData.AddrOfPinnedObject(), (nuint)size);
                    if (status != JxlDecoderStatus.Success)
                    {
                        throw new JxlDecodeException("Failed to set Exif box buffer.");
                    }
                }
                if (boxType == JxlBoxType.XMP)
                {
                    ulong size = 0;
                    status = JxlDecoderNativeMethod.JxlDecoderGetBoxSizeContents(_decoderPtr, ref size);
                    if (status != JxlDecoderStatus.Success)
                    {
                        throw new JxlDecodeException("Failed to get XMP box size.");
                    }
                    _xmpData = new byte[size];
                    _pinnedXmpData = GCHandle.Alloc(_xmpData, GCHandleType.Pinned);
                    status = JxlDecoderNativeMethod.JxlDecoderSetBoxBuffer(_decoderPtr, _pinnedXmpData.AddrOfPinnedObject(), (nuint)size);
                    if (status != JxlDecoderStatus.Success)
                    {
                        throw new JxlDecodeException("Failed to set XMP box buffer.");
                    }
                }
            }
            else if (status is JxlDecoderStatus.BoxNeedMoreOutput)
            {
                nuint remain = JxlDecoderNativeMethod.JxlDecoderReleaseBoxBuffer(_decoderPtr);
                if (boxType == JxlBoxType.Exif)
                {
                    int offset = _exifData!.Length - (int)remain;
                    _pinnedExifData.Free();
                    Array.Resize(ref _exifData, _exifData.Length * 2);
                    _pinnedExifData = GCHandle.Alloc(_exifData, GCHandleType.Pinned);
                    status = JxlDecoderNativeMethod.JxlDecoderSetBoxBuffer(_decoderPtr, _pinnedExifData.AddrOfPinnedObject() + offset, (nuint)(_exifData.Length - offset));
                    if (status != JxlDecoderStatus.Success)
                    {
                        throw new JxlDecodeException("Failed to set Exif box buffer.");
                    }
                }
                if (boxType == JxlBoxType.XMP)
                {
                    int offset = _xmpData!.Length - (int)remain;
                    _pinnedXmpData.Free();
                    Array.Resize(ref _xmpData, _xmpData.Length * 2);
                    _pinnedXmpData = GCHandle.Alloc(_xmpData, GCHandleType.Pinned);
                    status = JxlDecoderNativeMethod.JxlDecoderSetBoxBuffer(_decoderPtr, _pinnedXmpData.AddrOfPinnedObject() + offset, (nuint)(_xmpData.Length - offset));
                    if (status != JxlDecoderStatus.Success)
                    {
                        throw new JxlDecodeException("Failed to set XMP box buffer.");
                    }
                }
            }
            else if (status is JxlDecoderStatus.BoxComplete)
            {
                nuint remain = JxlDecoderNativeMethod.JxlDecoderReleaseBoxBuffer(_decoderPtr);
                if (boxType == JxlBoxType.Exif && _exifData != null)
                {
                    _pinnedExifData.Free();
                    Array.Resize(ref _exifData, _exifData.Length - (int)remain);
                }
                if (boxType == JxlBoxType.XMP && _xmpData != null)
                {
                    _pinnedXmpData.Free();
                    Array.Resize(ref _xmpData, _xmpData.Length - (int)remain);
                }
            }
            else if (status is JxlDecoderStatus.Error or JxlDecoderStatus.NeedMoreInput)
            {
                throw new JxlDecodeException("Decoding error.");
            }
            else
            {
                break;
            }
        }
    }


    /// <summary>
    ///  Exif data as a byte array.
    /// </summary>
    /// <returns></returns>
    public byte[]? GetExifData() => _exifData;


    /// <summary>
    /// XMP data as a byte array.
    /// </summary>
    /// <returns></returns>
    public byte[]? GetXMPData() => _xmpData;


    /// <summary>
    /// Gets the decoded pixel data as a byte array.
    /// </summary>
    /// <returns>The byte array containing the pixel data.</returns>
    /// <exception cref="JxlDecodeException">Thrown when the pixel buffer is null after processing.</exception>
    public byte[] GetPixelBytes()
    {
        try
        {
            if (_pixelBuffer != null)
            {
                return _pixelBuffer;
            }
            _decodePixelFormat = PixelFormat;
            ProcessImage();
            if (_pixelBuffer == null)
            {
                throw new JxlDecodeException("Pixel buffer is null.");
            }
            return _pixelBuffer;
        }
        finally
        {
            if (_pinnedPixelBuffer.IsAllocated)
            {
                _pinnedPixelBuffer.Free();
            }
        }
    }



    private JxlPixelFormat _decodePixelFormat;


    public byte[] GetPixelBytes(JxlPixelFormat pixelFormat)
    {
        try
        {
            if (_pixelBuffer != null)
            {
                return _pixelBuffer;
            }
            _decodePixelFormat = pixelFormat;
            ProcessImage();
            if (_pixelBuffer == null)
            {
                throw new JxlDecodeException("Pixel buffer is null.");
            }
            return _pixelBuffer;
        }
        finally
        {
            if (_pinnedPixelBuffer.IsAllocated)
            {
                _pinnedPixelBuffer.Free();
            }
        }
    }



    private void Rewind()
    {
        JxlDecoderNativeMethod.JxlDecoderRewind(_decoderPtr);
        JxlDecoderNativeMethod.JxlDecoderSubscribeEvents(_decoderPtr, JxlDecoderStatus.FullImage);
        JxlDecoderNativeMethod.JxlDecoderSetInput(_decoderPtr, _pinnedBytes.AddrOfPinnedObject(), (nuint)_inputBytesLength);
        JxlDecoderNativeMethod.JxlDecoderCloseInput(_decoderPtr);
    }


    /// <summary>
    /// Processes the input to get the full image.
    /// </summary>
    /// <exception cref="JxlDecodeException">Thrown when decoding fails.</exception>
    private void ProcessImage()
    {
        Rewind();
        while (true)
        {
            JxlDecoderStatus status = JxlDecoderNativeMethod.JxlDecoderProcessInput(_decoderPtr);
            if (status is JxlDecoderStatus.BasicInfo or JxlDecoderStatus.ColorEncoding)
            {
                continue;
            }
            else if (status is JxlDecoderStatus.NeedImageOutBuffer)
            {
                nuint bufferSize = 0;
                JxlDecoderNativeMethod.JxlDecoderImageOutBufferSize(_decoderPtr, _decodePixelFormat, ref bufferSize);
                _pixelBuffer = new byte[bufferSize];
                _pinnedPixelBuffer = GCHandle.Alloc(_pixelBuffer, GCHandleType.Pinned);
                unsafe
                {
                    status = JxlDecoderNativeMethod.JxlDecoderSetImageOutBuffer(_decoderPtr, _decodePixelFormat, _pinnedPixelBuffer.AddrOfPinnedObject(), bufferSize);
                    if (status != JxlDecoderStatus.Success)
                    {
                        _pinnedPixelBuffer.Free();
                        _pixelBuffer = null;
                        throw new JxlDecodeException("Failed to set image out buffer.");
                    }
                }
            }
            else if (status is JxlDecoderStatus.FullImage)
            {

            }
            else if (status is JxlDecoderStatus.Success)
            {
                break;
            }
            else if (status is JxlDecoderStatus.Error or JxlDecoderStatus.NeedMoreInput)
            {
                throw new JxlDecodeException("Decoding error.");
            }
            else
            {
                throw new JxlDecodeException("Unknown decoder error.");
            }
        }
    }




    private bool disposedValue;

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="JxlDecoderLite"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // 释放托管状态(托管对象)
                _pixelBuffer = null;
                _exifData = null;
                _xmpData = null;
            }

            // 释放未托管的资源(未托管的对象)并重写终结器
            // 将大型字段设置为 null
            if (_decoderPtr != IntPtr.Zero)
            {
                JxlDecoderNativeMethod.JxlDecoderDestroy(_decoderPtr);
                _decoderPtr = IntPtr.Zero;
            }
            if (_parallelRunnerPtr != IntPtr.Zero)
            {
                JxlParallelRunnerNativeMethod.JxlResizableParallelRunnerDestroy(_parallelRunnerPtr);
                _parallelRunnerPtr = IntPtr.Zero;
            }
            if (_pinnedBytes.IsAllocated)
            {
                _pinnedBytes.Free();
            }
            if (_pinnedExifData.IsAllocated)
            {
                _pinnedExifData.Free();
            }
            if (_pinnedXmpData.IsAllocated)
            {
                _pinnedXmpData.Free();
            }
            disposedValue = true;
        }
    }


    /// <summary>
    /// Finalizes an instance of the <see cref="JxlDecoderLite"/> class.
    /// </summary>
    ~JxlDecoderLite()
    {
        // 不要更改此代码。请将清理代码放入"Dispose(bool disposing)"方法中
        // 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        Dispose(disposing: false);
    }


    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="JxlDecoderLite"/> and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        // 不要更改此代码。请将清理代码放入"Dispose(bool disposing)"方法中
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }


}
