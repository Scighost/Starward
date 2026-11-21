using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Starward.Features.Gacha.Endfield;

internal static class EndfieldTokenProtector
{
    private const uint CryptProtectUiForbidden = 0x1;

    public static byte[] Protect(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Transform(System.Text.Encoding.UTF8.GetBytes(value), protect: true);
    }

    public static string Unprotect(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return System.Text.Encoding.UTF8.GetString(Transform(value, protect: false));
    }

    private static byte[] Transform(byte[] value, bool protect)
    {
        IntPtr inputPointer = Marshal.AllocHGlobal(value.Length);
        Marshal.Copy(value, 0, inputPointer, value.Length);
        var input = new DataBlob { Size = value.Length, Data = inputPointer };
        DataBlob output = default;
        IntPtr description = IntPtr.Zero;
        try
        {
            bool succeeded = protect
                ? CryptProtectData(ref input, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    CryptProtectUiForbidden, out output)
                : CryptUnprotectData(ref input, out description, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    CryptProtectUiForbidden, out output);
            if (!succeeded)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法安全保存鹰角账号登录信息。");
            }
            var result = new byte[output.Size];
            Marshal.Copy(output.Data, result, 0, output.Size);
            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(inputPointer);
            if (output.Data != IntPtr.Zero)
            {
                LocalFree(output.Data);
            }
            if (description != IntPtr.Zero)
            {
                LocalFree(description);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Size;
        public IntPtr Data;
    }

    [DllImport("Crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(ref DataBlob dataIn, string? description, IntPtr optionalEntropy,
        IntPtr reserved, IntPtr prompt, uint flags, out DataBlob dataOut);

    [DllImport("Crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(ref DataBlob dataIn, out IntPtr description, IntPtr optionalEntropy,
        IntPtr reserved, IntPtr prompt, uint flags, out DataBlob dataOut);

    [DllImport("Kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);
}
