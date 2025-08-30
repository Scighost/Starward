namespace Starward.Codec.UltraHdr;

public struct UhdrDecoderPtr
{
    private IntPtr _ptr;
    public static implicit operator IntPtr(UhdrDecoderPtr ptr) => ptr._ptr;
    public static implicit operator UhdrDecoderPtr(IntPtr ptr) => new UhdrDecoderPtr { _ptr = ptr };
}