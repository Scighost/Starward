using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

[InlineArray(256)]
public struct avifDiagnostics
{

    private byte _element0;
    // Upon receiving an error from any non-const libavif API call, if the toplevel structure used
    // in the API call (avifDecoder, avifEncoder) contains a diag member, this buffer may be
    // populated with a NULL-terminated, freeform error string explaining the first encountered error in
    // more detail. It will be cleared at the beginning of every non-const API call.
    //
    // Note: If an error string contains the "[Strict]" prefix, it means that you encountered an
    // error that only occurs during strict decoding. If you disable strict mode, you will no
    // longer encounter this error.


    public override unsafe string ToString()
    {
        return Marshal.PtrToStringAnsi((IntPtr)Unsafe.AsPointer(ref _element0)) ?? string.Empty;
    }


    public ReadOnlySpan<byte> AsSpan() => MemoryMarshal.CreateReadOnlySpan(ref _element0, 256);


}
