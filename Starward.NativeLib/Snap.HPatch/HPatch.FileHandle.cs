using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Snap.HPatch;


#pragma warning disable CS3016 // 作为特性参数的数组不符合 CLS



public static unsafe partial class HPatch
{


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static BOOL FileHandleRead(void* input, ulong position, byte* start, byte* end)
    {
        try
        {
            bool result = RandomAccessRead.Exactly(new(((FileHandleInput*)input)->Handle, ownsHandle: false), new(start, (int)(end - start)), (long)position);
            return result;
        }
        catch
        {
            return false;
        }
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static BOOL FileHandleWrite(void* output, ulong position, byte* start, byte* end)
    {
        try
        {
            RandomAccess.Write(new(((FileHandleOutput*)output)->Handle, ownsHandle: false), new ReadOnlySpan<byte>(start, (int)(end - start)), (long)position);
            return true;
        }
        catch
        {
            return false;
        }
    }




    internal readonly struct FileHandleInput
    {
        public readonly nint Handle;
        public readonly ulong Length;
        public readonly delegate* unmanaged[Cdecl]<void*, ulong, byte*, byte*, BOOL> Read;
        private readonly void* reserved;

        public FileHandleInput(SafeFileHandle? handle)
        {
            if (handle is not null)
            {
                Handle = handle.DangerousGetHandle();
                Length = (ulong)RandomAccess.GetLength(handle);
            }
            Read = &FileHandleRead;
        }
    }


    internal readonly struct FileHandleOutput
    {
        public readonly nint Handle;
        public readonly ulong Length;
        public readonly delegate* unmanaged[Cdecl]<void*, ulong, byte*, byte*, BOOL> Read;
        public readonly delegate* unmanaged[Cdecl]<void*, ulong, byte*, byte*, BOOL> Write;

        public FileHandleOutput(SafeFileHandle handle, ulong length)
        {
            Handle = handle.DangerousGetHandle();
            Length = length;
            Read = &FileHandleRead;
            Write = &FileHandleWrite;
        }
    }

}


