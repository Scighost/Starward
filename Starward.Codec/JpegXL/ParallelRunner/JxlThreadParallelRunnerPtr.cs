using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.ParallelRunner;

/// <summary>
/// Opaque structure that holds the JxlThreadParallelRunner.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlThreadParallelRunnerPtr
{
    private IntPtr _ptr;

    /// <summary>
    /// Implicit conversion to <see cref="IntPtr"/>.
    /// </summary>
    /// <param name="ptr"></param>
    public static implicit operator IntPtr(JxlThreadParallelRunnerPtr ptr) => ptr._ptr;

    /// <summary>
    /// Implicit conversion from <see cref="IntPtr"/>.
    /// </summary>
    /// <param name="ptr"></param>
    public static implicit operator JxlThreadParallelRunnerPtr(IntPtr ptr) => new() { _ptr = ptr };


    /// <summary>
    /// Gets the default thread parallel runner.
    /// </summary>
    /// <returns></returns>
    public static JxlThreadParallelRunnerPtr GetDefault()
    {
        return JxlParallelRunnerNativeMethod.JxlThreadParallelRunnerCreate(0, JxlParallelRunnerNativeMethod.JxlThreadParallelRunnerDefaultNumWorkerThreads());
    }

}
