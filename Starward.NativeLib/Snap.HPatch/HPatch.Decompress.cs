using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Snap.HPatch;


#pragma warning disable CS3016 // 作为特性参数的数组不符合 CLS



public static unsafe partial class HPatch
{
    internal struct Decompress
    {
        private readonly delegate* unmanaged[Cdecl]<PCSTR, BOOL> isCanOpen;
        private readonly delegate* unmanaged[Cdecl]<Decompress*, ulong, FileHandleInput*, ulong, ulong, nint> open;
        private readonly delegate* unmanaged[Cdecl]<Decompress*, nint, BOOL> close;
        private readonly delegate* unmanaged[Cdecl]<nint, byte*, byte*, BOOL> decompress;
        private readonly delegate* unmanaged[Cdecl]<nint, ulong, FileHandleInput*, ulong, ulong, BOOL> reset;
        private int error;

        public Decompress(
            delegate* unmanaged[Cdecl]<PCSTR, BOOL> isCanOpen,
            delegate* unmanaged[Cdecl]<Decompress*, ulong, FileHandleInput*, ulong, ulong, nint> open,
            delegate* unmanaged[Cdecl]<Decompress*, nint, BOOL> close,
            delegate* unmanaged[Cdecl]<nint, byte*, byte*, BOOL> decompress,
            delegate* unmanaged[Cdecl]<nint, ulong, FileHandleInput*, ulong, ulong, BOOL> reset)
        {
            this.isCanOpen = isCanOpen;
            this.open = open;
            this.close = close;
            this.decompress = decompress;
            this.reset = reset;
        }

        public static Decompress CreateZstandard()
        {
            return new(&ZstandardIsCanOpen, &ZstandardOpen, &ZstandardClose, &ZstandardDecompress, null);
        }
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static BOOL ZstandardIsCanOpen(PCSTR compressType)
    {
        return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(compressType).SequenceEqual("zstd"u8);
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static nint ZstandardOpen(Decompress* decompressor, ulong dataSize, FileHandleInput* codeStream, ulong codeBegin, ulong codeEnd)
    {
        ZstdSharp.DecompressionStream stream = new(new InputSliceStream(codeStream, codeBegin, codeEnd), leaveOpen: false);
        return GCHandle.ToIntPtr(GCHandle.Alloc(stream));
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static BOOL ZstandardClose(Decompress* decompressor, nint handle)
    {
        GCHandle gcHandle = GCHandle.FromIntPtr(handle);
        if (gcHandle.Target is not ZstdSharp.DecompressionStream stream)
        {
            return true;
        }

        stream.Dispose();
        gcHandle.Free();
        return true;
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static BOOL ZstandardDecompress(nint handle, byte* data, byte* dataEnd)
    {
        GCHandle gcHandle = GCHandle.FromIntPtr(handle);
        if (gcHandle.Target is not ZstdSharp.DecompressionStream stream)
        {
            return false;
        }

        try
        {
            stream.ReadExactly(new(data, (int)(dataEnd - data)));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
