namespace Starward.Codec.UltraHdr;

public abstract class UhdrCodec : IDisposable
{

    public static string Version => "1.4.0";

    public static string CommitHash => "5fa99b5271a3c80a13c78062d7adc6310222dd8e";


    protected IntPtr _codecHandle;


    /// <summary>
    /// check if it is a valid ultrahdr image.
    /// True if the input data has a primary image, gain map image and gain map metadata. False if any
    /// errors are encountered during parsing process or if the image does not have primary
    /// image or gainmap image or gainmap metadata
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static unsafe bool IsUhdrImage(ReadOnlySpan<byte> bytes)
    {
        fixed (byte* p = bytes)
        {
            int uhdr = UhdrNativeMethod.is_uhdr_image((nint)p, bytes.Length);
            return uhdr != 0;
        }
    }


    /// <summary>
    /// Enable/Disable GPU acceleration.
    /// If enabled, certain operations (if possible) of uhdr encode/decode will be offloaded to GPU.
    /// NOTE: It is entirely possible for this API to have no effect on the encode/decode operation
    /// </summary>
    /// <param name="enable">enable/disbale gpu acceleration</param>
    public void EnableGpuAcceleration(bool enable)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enable_gpu_acceleration(_codecHandle, enable ? 1 : 0);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Add mirror effect.
    /// </summary>
    /// <param name="direction">MirrorVertical or MirrorHorizontal</param>
    public void AddEffectMirror(UhdrMirrorDirection direction)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_add_effect_mirror(_codecHandle, direction);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Add rotate effect.<br/>
    /// 90 - rotate clockwise by 90 degrees<br/>
    /// 180 - rotate clockwise by 180 degrees<br/>
    /// 270 - rotate clockwise by 270 degrees
    /// </summary>
    /// <param name="degrees">clockwise degrees</param>
    public void AddEffectRotate(int degrees)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_add_effect_rotate(_codecHandle, degrees);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Add crop effect. Crop coordinate in pixels.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="top"></param>
    /// <param name="bottom"></param>
    public void AddEffectCrop(int left, int right, int top, int bottom)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_add_effect_crop(_codecHandle, left, right, top, bottom);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Add resize effect.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public void AddEffectResize(int width, int height)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_add_effect_resize(_codecHandle, width, height);
        errorInfo.ThrowIfError();
    }



    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {

            }
            UhdrNativeMethod.uhdr_release_encoder(_codecHandle);
            _codecHandle = IntPtr.Zero;
            disposedValue = true;
        }
    }

    ~UhdrCodec()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }


}