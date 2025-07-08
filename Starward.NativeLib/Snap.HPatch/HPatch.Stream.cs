using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Snap.HPatch;

#pragma warning disable CS3016 // 作为特性参数的数组不符合 CLS


public static unsafe partial class HPatch
{


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static BOOL StreamRead(void* input, ulong position, byte* start, byte* end)
    {
        GCHandle gcHandle = GCHandle.FromIntPtr(((StreamInput*)input)->Handle);
        if (gcHandle.Target is not Stream stream)
        {
            return false;
        }
        stream.Position = (long)position;
        stream.ReadExactly(new Span<byte>(start, (int)(end - start)));
        return true;
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static BOOL StreamWrite(void* output, ulong position, byte* start, byte* end)
    {
        GCHandle gcHandle = GCHandle.FromIntPtr(((StreamOutput*)output)->Handle);
        if (gcHandle.Target is not Stream stream)
        {
            return false;
        }
        stream.Position = (long)position;
        stream.Write(new ReadOnlySpan<byte>(start, (int)(end - start)));
        return true;
    }




    internal struct StreamInput : IDisposable
    {
        public nint Handle;
        public readonly ulong Length;
        public readonly delegate* unmanaged[Cdecl]<void*, ulong, byte*, byte*, BOOL> Read;
        private readonly void* reserved;

        public StreamInput(Stream? stream)
        {
            if (stream is not null)
            {
                Handle = GCHandle.ToIntPtr(GCHandle.Alloc(stream));
                Length = (ulong)stream.Length;
            }
            Read = &StreamRead;
        }

        public void Dispose()
        {
            if (Handle is not 0)
            {
                GCHandle gcHandle = GCHandle.FromIntPtr(Handle);
                gcHandle.Free();
                Handle = 0;
            }
        }
    }


    internal struct StreamOutput : IDisposable
    {
        public nint Handle;
        public readonly ulong Length;
        public readonly delegate* unmanaged[Cdecl]<void*, ulong, byte*, byte*, BOOL> Read;
        public readonly delegate* unmanaged[Cdecl]<void*, ulong, byte*, byte*, BOOL> Write;

        public StreamOutput(Stream stream, ulong length)
        {
            Handle = GCHandle.ToIntPtr(GCHandle.Alloc(stream));
            Length = length;
            Read = &StreamRead;
            Write = &StreamWrite;
        }

        public void Dispose()
        {
            if (Handle is not 0)
            {
                GCHandle gcHandle = GCHandle.FromIntPtr(Handle);
                gcHandle.Free();
                Handle = 0;
            }
        }
    }

}
