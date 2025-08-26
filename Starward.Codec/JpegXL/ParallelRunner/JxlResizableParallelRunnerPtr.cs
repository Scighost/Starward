using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.ParallelRunner;

/// <summary>
/// Opaque structure that holds the JxlResizableParallelRunner.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlResizableParallelRunnerPtr
{
    private IntPtr _ptr;

    /// <summary>
    /// Implicit conversion to <see cref="IntPtr"/>.
    /// </summary>
    /// <param name="ptr"></param>
    public static implicit operator IntPtr(JxlResizableParallelRunnerPtr ptr) => ptr._ptr;

    /// <summary>
    /// Implicit conversion from <see cref="IntPtr"/>.
    /// </summary>
    /// <param name="ptr"></param>
    public static implicit operator JxlResizableParallelRunnerPtr(IntPtr ptr) => new() { _ptr = ptr };


    /// <summary>
    /// Gets the default resizable parallel runner.
    /// </summary>
    /// <returns></returns>
    public static JxlResizableParallelRunnerPtr GetDefault()
    {
        return JxlParallelRunnerNativeMethod.JxlResizableParallelRunnerCreate(0);
    }

}
