using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Snap.HPatch;


#pragma warning disable CS3001 // 参数类型不符合 CLS
#pragma warning disable CS3016 // 作为特性参数的数组不符合 CLS


public static unsafe partial class HPatch
{

    public static bool Patch(SafeFileHandle? source, SafeFileHandle diff, SafeFileHandle target)
    {
        FileHandleInput sourceAdapter = new(source);
        FileHandleInput diffAdapter = new(diff);
        ulong newDataSize;
        if (!NewDataSize(&diffAdapter, &newDataSize))
        {
            return false;
        }
        FileHandleOutput targetAdapter = new(target, newDataSize);
        return Patch(&sourceAdapter, &diffAdapter, &targetAdapter);
    }


    public static bool PatchZstandard(SafeFileHandle? source, SafeFileHandle diff, SafeFileHandle target)
    {
        FileHandleInput sourceAdapter = new(source);
        FileHandleInput diffAdapter = new(diff);
        ulong newDataSize;
        if (!NewDataSize(&diffAdapter, &newDataSize))
        {
            return false;
        }
        FileHandleOutput targetAdapter = new(target, newDataSize);
        Decompress decompressor = Decompress.CreateZstandard();
        return PatchWithDecompressor(&sourceAdapter, &diffAdapter, &targetAdapter, &decompressor);
    }



    public static bool Patch(Stream? source, Stream diff, Stream target)
    {
        using StreamInput sourceAdapter = new(source);
        using StreamInput diffAdapter = new(diff);
        ulong newDataSize;
        if (!NewDataSize(&diffAdapter, &newDataSize))
        {
            return false;
        }
        using StreamOutput targetAdapter = new(target, newDataSize);
        return Patch(&sourceAdapter, &diffAdapter, &targetAdapter);
    }


    public static bool PatchZstandard(Stream? source, Stream diff, Stream target)
    {
        using StreamInput sourceAdapter = new(source);
        using StreamInput diffAdapter = new(diff);
        ulong newDataSize;
        if (!NewDataSize(&diffAdapter, &newDataSize))
        {
            return false;
        }
        using StreamOutput targetAdapter = new(target, newDataSize);
        Decompress decompressor = Decompress.CreateZstandard();
        return PatchWithDecompressor(&sourceAdapter, &diffAdapter, &targetAdapter, &decompressor);
    }




    [LibraryImport("Snap.HPatch.dll")]
    private static partial BOOL NewDataSize(FileHandleInput* diff, ulong* pSize);

    [LibraryImport("Snap.HPatch.dll")]
    private static partial BOOL NewDataSize(StreamInput* diff, ulong* pSize);

    [LibraryImport("Snap.HPatch.dll")]
    private static partial BOOL Patch(FileHandleInput* source, FileHandleInput* diff, FileHandleOutput* target);

    [LibraryImport("Snap.HPatch.dll")]
    private static partial BOOL Patch(StreamInput* source, StreamInput* diff, StreamOutput* target);

    [LibraryImport("Snap.HPatch.dll")]
    private static partial BOOL PatchWithDecompressor(FileHandleInput* source, FileHandleInput* diff, FileHandleOutput* target, Decompress* decompressor);

    [LibraryImport("Snap.HPatch.dll")]
    private static partial BOOL PatchWithDecompressor(StreamInput* source, StreamInput* diff, StreamOutput* target, Decompress* decompressor);


}
