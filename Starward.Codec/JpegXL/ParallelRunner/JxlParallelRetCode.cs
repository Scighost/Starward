namespace Starward.Codec.JpegXL.ParallelRunner;

/// <summary>
/// Return code used in the JxlParallel* functions as return value.
/// </summary>
public enum JxlParallelRetCode
{
    /// <summary>
    /// Code returned by the <see cref="JxlParallelRunnerNativeMethod.JxlParallelRunInit"/> function to indicate success.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Code returned by the <see cref="JxlParallelRunnerNativeMethod.JxlParallelRunInit"/> function to indicate a general error.
    /// </summary>
    Error = -1,
}