using Starward.Codec.JpegXL.CMS;
using Starward.Codec.JpegXL.ParallelRunner;
using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.Decode;

[Obsolete("Not finished.", true)]
public class JxlDecoder : IDisposable
{

    public static Version Version => GetDecoderVersion();


    private static Version GetDecoderVersion()
    {
        uint version = JxlDecoderNativeMethod.JxlDecoderVersion();
        uint patch = version % 1000;
        uint minor = (version / 1000) % 1000;
        uint major = version / 1000000;
        return new Version((int)major, (int)minor, (int)patch);
    }



    public static unsafe JxlSignature CheckSignature(ReadOnlySpan<byte> buffer)
    {
        fixed (byte* p = buffer)
        {
            return JxlDecoderNativeMethod.JxlSignatureCheck((nint)p, (nuint)buffer.Length);
        }
    }


    public static JxlSignature CheckSignature(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[12];
        int bytesRead = stream.Read(buffer);
        return CheckSignature(buffer.Slice(0, bytesRead));
    }



    private JxlDecoderPtr _decoderPtr;

    private JxlThreadParallelRunnerPtr _parallelRunnerPtr;

    private IntPtr _jxlParallelRunnerFunction;

    private JxlCmsInterface _cmsInterface;


    public JxlDecoder()
    {
        _decoderPtr = JxlDecoderNativeMethod.JxlDecoderCreate();
        if (_decoderPtr == IntPtr.Zero)
        {
            throw new JxlDecodeException("Failed to create JxlDecoder.");
        }
        _parallelRunnerPtr = JxlThreadParallelRunnerPtr.GetDefault();
        _jxlParallelRunnerFunction = JxlParallelRunnerNativeMethod.GetJxlThreadParallelRunner();
        _cmsInterface = JxlCmsInterface.GetDefault();
        JxlDecoderNativeMethod.JxlDecoderSetParallelRunner(_decoderPtr, _jxlParallelRunnerFunction, _parallelRunnerPtr);
        JxlDecoderNativeMethod.JxlDecoderSetCms(_decoderPtr, _cmsInterface);
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

            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
            if (_decoderPtr != IntPtr.Zero)
            {
                JxlDecoderNativeMethod.JxlDecoderDestroy(_decoderPtr);
                _decoderPtr = IntPtr.Zero;
            }
            if (_parallelRunnerPtr != IntPtr.Zero)
            {
                JxlParallelRunnerNativeMethod.JxlThreadParallelRunnerDestroy(_parallelRunnerPtr);
                _parallelRunnerPtr = IntPtr.Zero;
            }
            if (_jxlParallelRunnerFunction != IntPtr.Zero)
            {
                NativeLibrary.Free(_jxlParallelRunnerFunction);
                _jxlParallelRunnerFunction = IntPtr.Zero;
            }
            disposedValue = true;
        }
    }

    // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
    ~JxlDecoder()
    {
        // 不要更改此代码。请将清理代码放入"Dispose(bool disposing)"方法中
        Dispose(disposing: false);
    }


    public void Dispose()
    {
        // 不要更改此代码。请将清理代码放入"Dispose(bool disposing)"方法中
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
