using System.Runtime.InteropServices;

namespace Starward.Codec.UltraHdr;

/// <summary>
/// Detailed return status
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct UhdrErrorInfo
{
    /// <summary>
    /// error code
    /// </summary>
    public UhdrCodecError ErrorCode;
    /// <summary>
    /// has detailed error logs. 0 - no, else - yes
    /// </summary>
    public int hasDetail;
    /// <summary>
    /// error logs
    /// </summary>
    public fixed byte detail[256];


    public bool Success => ErrorCode == UhdrCodecError.OK;

    public string? Detail => GetDetail();


    private string? GetDetail()
    {
        if (hasDetail is 0)
        {
            return null;
        }
        else
        {
            fixed (byte* p = detail)
            {
                return Marshal.PtrToStringAnsi((nint)p);
            }
        }
    }


    public void ThrowIfError()
    {
        if (!Success)
        {
            string message = ErrorCode switch
            {
                UhdrCodecError.Error => "Generic codec error.",
                UhdrCodecError.UnknownError => "Unknown error.",
                UhdrCodecError.InvalidParameter => "An application-supplied parameter is not valid.",
                UhdrCodecError.MemoryError => "Memory operation failed.",
                UhdrCodecError.InvalidOperation => "An application-invoked operation is not valid.",
                UhdrCodecError.UnsupportedFeature => "The library does not implement a feature required for the operation.",
                UhdrCodecError.ListEnd => "Not for usage, indicates end of list.",
                _ => "AUnknown error.",
            };
            if (hasDetail != 0)
            {
                message += $" {GetDetail()}";
            }
            throw new UhdrException(this, message);
        }
    }


}
