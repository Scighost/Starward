using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.Encode;

/// <summary>
/// The <see cref="JxlEncoderOutputProcessor"/> structure provides an interface for the encoder's output processing.
/// <para>Users of the library, who want to do streaming encoding, should implement the required callbacks for buffering, writing, seeking (if supported), and setting a finalized position during the encoding process.</para>
/// <para>At a high level, the processor can be in one of two states:</para>
/// <list type="bullet">
/// <item><description>With an active buffer: This indicates that a buffer has been acquired using <c>get_buffer</c> and encoded data can be written to it.</description></item>
/// <item><description>Without an active buffer: In this state, no data can be written. A new buffer must be acquired after releasing any previously active buffer.</description></item>
/// </list>
/// <para>The library will not acquire more than one buffer at a given time.</para>
/// <para>The state of the processor includes position and finalized position, which have the following meaning.</para>
/// <list type="bullet">
/// <item><description>position: Represents the current position, in bytes, within the output stream where the encoded data will be written next. This position moves forward with each <c>release_buffer</c> call as data is written, and can also be adjusted through the optional seek callback, if provided. At this position the next write will occur.</description></item>
/// <item><description>finalized position: A position in the output stream that ensures all bytes before this point are finalized and won't be changed by later writes.</description></item>
/// </list>
/// <para>All fields but <see cref="seek"/> are required, <see cref="seek"/> is optional and can be <see langword="null"/>.</para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlEncoderOutputProcessor
{
    /// <summary>
    /// Required.
    /// <para>An opaque pointer that the client can use to store custom data.</para>
    /// <para>This data will be passed to the associated callback functions.</para>
    /// </summary>
    public IntPtr opaque;

    /// <summary>
    /// <see cref="JpegxlGetBufferFunc"/>
    /// <para>Required.</para>
    /// <para>Acquires a buffer at the current position into which the library will write the output data.</para>
    /// </summary>
    public unsafe delegate* unmanaged[Cdecl]<void*, nuint*, void*> get_buffer;

    /// <summary>
    /// <see cref="JpegxlReleaseBufferFunc"/>
    /// <para>Notifies the user of library that the current buffer's data has been written and can be released.</para>
    /// <para>This function should advance the current position of the buffer by <c>written_bytes</c> number of bytes.</para>
    /// </summary>
    public unsafe delegate* unmanaged[Cdecl]<void*, nuint, void> release_buffer;

    /// <summary>
    /// <see cref="JpegxlSeekFunc"/>
    /// <para>Seeks to a specific position in the output. This function is optional and can be set to <see langword="null"/> if the output doesn't support seeking.</para>
    /// <para>Can only be done when there is no buffer.</para>
    /// <para>Cannot be used to seek before the finalized position.</para>
    /// </summary>
    public unsafe delegate* unmanaged[Cdecl]<void*, ulong, void> seek;

    /// <summary>
    /// <see cref="JpegxlSetFinalizedPositionFunc"/>
    /// <para>Sets a finalized position on the output data, at a specific position.</para>
    /// <para>Seeking will never request a position before the finalized position.</para>
    /// <para>Will only be called if there is no active buffer.</para>
    /// </summary>
    public unsafe delegate* unmanaged[Cdecl]<void*, ulong, void> set_finalized_position;


    /// <summary>
    /// Acquires a buffer at the current position into which the library will write the output data.
    /// </summary>
    /// <param name="opaque">user supplied parameters to the callback</param>
    /// <param name="size">points to a suggested buffer size when called; must be set to the size of the returned buffer once the function returns.</param>
    /// <returns>a pointer to the acquired buffer or <see langword="null"/> to indicate a stop condition.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void* JpegxlGetBufferFunc(void* opaque, nuint* size);


    /// <summary>
    /// Notifies the user of library that the current buffer's data has been written and can be released.
    /// <para>This function should advance the current position of the buffer by <c>written_bytes</c> number of bytes.</para>
    /// </summary>
    /// <param name="opaque">user supplied parameters to the callback</param>
    /// <param name="written_bytes">the number of bytes written to the buffer.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void JpegxlReleaseBufferFunc(void* opaque, nuint written_bytes);


    /// <summary>
    /// Seeks to a specific position in the output.
    /// <para>This function is optional and can be set to <see langword="null"/> if the output doesn't support seeking.</para>
    /// <para>Can only be done when there is no buffer.</para>
    /// <para>Cannot be used to seek before the finalized position.</para>
    /// </summary>
    /// <param name="opaque">user supplied parameters to the callback</param>
    /// <param name="position">the position to seek to, in bytes.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void JpegxlSeekFunc(void* opaque, ulong position);


    /// <summary>
    /// Sets a finalized position on the output data, at a specific position.
    /// <para>Seeking will never request a position before the finalized position.</para>
    /// <para>Will only be called if there is no active buffer.</para>
    /// </summary>
    /// <param name="opaque">user supplied parameters to the callback</param>
    /// <param name="finalized_position">the position, in bytes, where the finalized position should be set.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void JpegxlSetFinalizedPositionFunc(void* opaque, ulong finalized_position);
}
