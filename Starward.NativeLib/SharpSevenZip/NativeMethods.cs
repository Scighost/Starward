using System.Runtime.InteropServices;
using System.Security;

namespace SharpSevenZip;

[SecurityCritical, SuppressUnmanagedCodeSecurity]
internal static class NativeMethods
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int CreateObjectDelegate(
        [In] ref Guid classID,
        [In] ref Guid interfaceID,
        [MarshalAs(UnmanagedType.Interface)] out object outObject);

    [DllImport("kernel32.dll", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string fileName);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", BestFitMapping = false, ThrowOnUnmappableChar = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

    public static T? SafeCast<T>(PropVariant var, T? def)
    {
        object? obj;

        try
        {
            obj = var.Object;
        }
        catch (Exception)
        {
            return def;
        }

        if (obj is T expected)
        {
            return expected;
        }

        return def;
    }
}
