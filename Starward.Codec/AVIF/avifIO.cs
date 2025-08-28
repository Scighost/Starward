using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

[StructLayout(LayoutKind.Sequential)]
public struct avifIO
{
    public avifIODestroyFunc destroy;
    public avifIOReadFunc read;

    // This is reserved for future use - but currently ignored. Set it to a null pointer.
    public avifIOWriteFunc write;

    // If non-zero, this is a hint to internal structures of the max size offered by the content
    // this avifIO structure is reading. If it is a static memory source, it should be the size of
    // the memory buffer; if it is a file, it should be the file's size. If this information cannot
    // be known (as it is streamed-in), set a reasonable upper boundary here (larger than the file
    // can possibly be for your environment, but within your environment's memory constraints). This
    // is used for sanity checks when allocating internal buffers to protect against
    // malformed/malicious files.
    public ulong sizeHint;

    // If true, *all* memory regions returned from *all* calls to read are guaranteed to be
    // persistent and exist for the lifetime of the avifIO object. If false, libavif will make
    // in-memory copies of samples and metadata content, and a memory region returned from read must
    // only persist until the next call to read.
    public avifBool persistent;

    // The contents of this are defined by the avifIO implementation, and should be fully destroyed
    // by the implementation of the associated destroy function, unless it isn't owned by the avifIO
    // struct. It is not necessary to use this pointer in your implementation.
    public unsafe void* data;
}


/// <summary>
/// Destroy must completely destroy all child structures *and* free the avifIO object itself.
/// This function pointer is optional, however, if the avifIO object isn't intended to be owned by
/// a libavif encoder/decoder.
/// </summary>
/// <param name="io"></param>
public unsafe delegate void avifIODestroyFunc(avifIO* io);


/// <summary>
/// This function should return a block of memory that *must* remain valid until another read call to
/// this avifIO struct is made (reusing a read buffer is acceptable/expected).
/// <para/>
/// <list type="bullet">
/// <item>If offset exceeds the size of the content (past EOF), return AVIF_RESULT_IO_ERROR.</item>
/// <item>If offset is *exactly* at EOF, provide a 0-byte buffer and return AVIF_RESULT_OK.</item>
/// <item>If (offset+size) exceeds the contents' size, it must truncate the range to provide all bytes from the offset to EOF.</item>
/// <item>If the range is unavailable yet (due to network conditions or any other reason), return AVIF_RESULT_WAITING_ON_IO.</item>
/// <item>Otherwise, provide the range and return AVIF_RESULT_OK.</item>
/// </list>
/// </summary>
/// <param name="io"></param>
/// <param name="readFlags"></param>
/// <param name="offset"></param>
/// <param name="size"></param>
/// <param name="outData"></param>
/// <returns></returns>
public unsafe delegate avifResult avifIOReadFunc(avifIO* io, uint readFlags, ulong offset, nuint size, avifROData* outData);



public unsafe delegate avifResult avifIOWriteFunc(avifIO* io, uint writeFlags, ulong offset, IntPtr data, nuint size);