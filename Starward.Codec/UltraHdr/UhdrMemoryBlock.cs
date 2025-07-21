using System.Runtime.InteropServices;

namespace Starward.Codec.UltraHdr;

/// <summary>
/// Buffer Descriptor
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct UhdrMemoryBlock
{
    /// <summary>
    /// Pointer to a block of data to decode
    /// </summary>
    public IntPtr Data;
    /// <summary>
    /// size of the data buffer
    /// </summary>
    public ulong DataSize;
    /// <summary>
    /// maximum size of the data buffer
    /// </summary>
    public ulong Capacity;


    public unsafe ReadOnlySpan<byte> AsSpan()
    {
        if (Data == IntPtr.Zero || DataSize == 0)
        {
            return ReadOnlySpan<byte>.Empty;
        }
        return new ReadOnlySpan<byte>((void*)Data, (int)DataSize);
    }

}
