namespace Starward.Codec.UltraHdr;

public struct UhdrEncoderPtr
{
    private IntPtr _ptr;
    public static implicit operator IntPtr(UhdrEncoderPtr ptr) => ptr._ptr;
    public static implicit operator UhdrEncoderPtr(IntPtr ptr) => new UhdrEncoderPtr { _ptr = ptr };
}