using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

[StructLayout(LayoutKind.Sequential)]
public record struct CString
{

    private readonly IntPtr _ptr;

    public int Length => _ptr == IntPtr.Zero ? 0 : GetLength();

    public unsafe ReadOnlySpan<byte> AsSpan() => _ptr == IntPtr.Zero ? ReadOnlySpan<byte>.Empty : MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)_ptr);


    public override string ToString() => Marshal.PtrToStringAnsi(_ptr) ?? string.Empty;


    private unsafe int GetLength()
    {
        if (_ptr == IntPtr.Zero)
        {
            return 0;
        }
        int i;
        for (i = 0; i < int.MaxValue; i++)
        {
            if (*(byte*)(_ptr + i) == 0)
            {
                break;
            }
        }
        return i;
    }


}