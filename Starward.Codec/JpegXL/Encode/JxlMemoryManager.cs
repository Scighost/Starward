using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.Encode;

/// <summary>
/// Memory Manager struct.
/// These functions, when provided by the caller, will be used to handle memory allocations.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlMemoryManager
{
    /// <summary>
    /// The opaque pointer that will be passed as the first parameter to all the functions in this struct.
    /// </summary>
    public IntPtr opaque;

    /// <summary>
    /// Memory allocation function. This can be NULL if and only if also the
    /// free() member in this class is NULL. All dynamic memory will be allocated
    /// and freed with these functions if they are not NULL, otherwise with the
    /// standard malloc/free. <see cref="JpegxlAllocFunc"/>
    /// </summary>
    public unsafe delegate* unmanaged[Cdecl]<IntPtr, nuint, IntPtr> alloc;

    /// <summary>
    /// Free function matching the alloc() member. <see cref="JpegxlFreeFunc"/>"/>
    /// </summary>
    public unsafe delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> free;


    /// <summary>
    /// Allocating function for a memory region of a given size.
    /// Allocates a contiguous memory region of size <paramref name="size"/> bytes. The returned
    /// memory may not be aligned to a specific size or initialized at all.
    /// </summary>
    /// <param name="opaque">custom memory manager handle provided by the caller.</param>
    /// <param name="size">in bytes of the requested memory region.</param>
    /// <returns>NULL if the memory can not be allocated, pointer to the memory otherwise.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void* JpegxlAllocFunc(void* opaque, nuint size);


    /// <summary>
    /// Deallocating function pointer type.
    /// This function MUST do nothing if <paramref name="address"/> is NULL.
    /// </summary>
    /// <param name="opaque">custom memory manager handle provided by the caller.</param>
    /// <param name="address">memory region pointer returned by ::jpegxl_alloc_func, or NULL</param>

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void JpegxlFreeFunc(void* opaque, void* address);

}


/// <summary>
/// Memory Manager struct.
/// These functions, when provided by the caller, will be used to handle memory allocations.
/// </summary>
public struct JxlMemoryManagerPtr
{
    private IntPtr _ptr;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ptr">The JxlMemoryManager instance.</param>
    public static implicit operator IntPtr(JxlMemoryManagerPtr ptr) => ptr._ptr;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ptr">The IntPtr instance.</param>
    public static implicit operator JxlMemoryManagerPtr(IntPtr ptr) => new JxlMemoryManagerPtr { _ptr = ptr };
}
