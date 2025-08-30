using Starward.Codec.JpegXL.Encode;
using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.ParallelRunner;

/// <summary>
/// API for running data operations in parallel in a multi-threaded environment.
/// This module allows the JPEG XL caller to define their own way of creating and assigning threads.
/// </summary>
public static partial class JxlParallelRunnerNativeMethod
{

    private const string LibraryName = "jxl_threads";


    /// <summary>
    /// Parallel runner internally using <c>std::thread</c>. Use as <see cref="JxlParallelRunner"/>.
    /// </summary>
    /// <param name="runner_opaque">Opaque pointer for the runner. For <see cref="JxlThreadParallelRunner"/>, this is the pointer returned by <see cref="JxlThreadParallelRunnerCreate"/>.</param>
    /// <param name="jpegxl_opaque">Opaque pointer for the JPEG XL caller. This must be passed to the <paramref name="init"/> and <paramref name="func"/> callbacks.</param>
    /// <param name="init">Initialization function. See <see cref="JxlParallelRunInit"/>.</param>
    /// <param name="func">Function to be executed in parallel. See <see cref="JxlParallelRunFunction"/>.</param>
    /// <param name="start_range">The start of the range for the parallel execution (inclusive).</param>
    /// <param name="end_range">The end of the range for the parallel execution (exclusive).</param>
    /// <returns>A status code indicating success or failure. See <see cref="JxlParallelRetCode"/>.</returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial JxlParallelRetCode JxlThreadParallelRunner(JxlThreadParallelRunnerPtr runner_opaque, void* jpegxl_opaque, JxlParallelRunInit init, JxlParallelRunFunction func, uint start_range, uint end_range);

    /// <summary>
    /// Creates the runner for <see cref="JxlThreadParallelRunner"/>. Use as the opaque runner.
    /// </summary>
    /// <param name="memory_manager">Opaque pointer to a memory manager. Can be <see langword="null"/> to use the default.</param>
    /// <param name="num_worker_threads">The number of threads to use for the parallel execution.</param>   
    /// <returns>An opaque pointer to the created runner, or <see langword="null"/> on failure.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlThreadParallelRunnerPtr JxlThreadParallelRunnerCreate(JxlMemoryManagerPtr memory_manager, nuint num_worker_threads);

    /// <summary>
    /// Destroys the runner created by <see cref="JxlThreadParallelRunnerCreate"/>.
    /// </summary>
    /// <param name="runner_opaque">The runner to destroy.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlThreadParallelRunnerDestroy(JxlThreadParallelRunnerPtr runner_opaque);

    /// <summary>
    /// Returns a default num_worker_threads value for <see cref="JxlThreadParallelRunnerCreate"/>.
    /// </summary>
    /// <returns>The default number of worker threads.</returns>
    [LibraryImport(LibraryName)]
    public static partial nuint JxlThreadParallelRunnerDefaultNumWorkerThreads();

    /// <summary>
    /// Get function pointer of <see cref="JxlThreadParallelRunner"/>.
    /// </summary>
    /// <returns>Function pointer</returns>
    public static IntPtr GetJxlThreadParallelRunner()
    {
        return NativeLibrary.GetExport(NativeLibrary.Load(LibraryName), nameof(JxlThreadParallelRunner));
    }


    /// <summary>
    /// Parallel runner internally using <c>std::thread</c>. Use as <see cref="JxlParallelRunner"/>.
    /// </summary>
    /// <param name="runner_opaque">Opaque pointer for the runner. For <see cref="JxlResizableParallelRunner"/>, this is the pointer returned by <see cref="JxlResizableParallelRunnerCreate"/>.</param>
    /// <param name="jpegxl_opaque">Opaque pointer for the JPEG XL caller. This must be passed to the <paramref name="init"/> and <paramref name="func"/> callbacks.</param>
    /// <param name="init">Initialization function. See <see cref="JxlParallelRunInit"/>.</param>
    /// <param name="func">Function to be executed in parallel. See <see cref="JxlParallelRunFunction"/>.</param>
    /// <param name="start_range">The start of the range for the parallel execution (inclusive).</param>
    /// <param name="end_range">The end of the range for the parallel execution (exclusive).</param>
    /// <returns>A status code indicating success or failure. See <see cref="JxlParallelRetCode"/>.</returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial JxlParallelRetCode JxlResizableParallelRunner(JxlResizableParallelRunnerPtr runner_opaque, void* jpegxl_opaque, JxlParallelRunInit init, JxlParallelRunFunction func, uint start_range, uint end_range);

    /// <summary>
    /// Creates the runner for <see cref="JxlResizableParallelRunner"/>. Use as the opaque
    /// runner. The runner will execute tasks on the calling thread until
    /// <see cref="JxlResizableParallelRunnerSetThreads"/> is called.
    /// </summary>
    /// <param name="memory_manager">Opaque pointer to a memory manager. Can be <see langword="null"/> to use the default.</param>
    /// <returns>An opaque pointer to the created runner, or <see langword="null"/> on failure.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlResizableParallelRunnerPtr JxlResizableParallelRunnerCreate(JxlMemoryManagerPtr memory_manager);

    /// <summary>
    /// Changes the number of threads for <see cref="JxlResizableParallelRunner"/>.
    /// </summary>
    /// <param name="runner_opaque">The runner instance.</param>
    /// <param name="num_threads">The new number of threads.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlResizableParallelRunnerSetThreads(JxlResizableParallelRunnerPtr runner_opaque, nuint num_threads);

    /// <summary>
    /// Suggests a number of threads to use for an image of given size.
    /// </summary>
    /// <param name="xsize">Image width.</param>
    /// <param name="ysize">Image height.</param>
    /// <returns>The suggested number of threads.</returns>
    [LibraryImport(LibraryName)]
    public static partial uint JxlResizableParallelRunnerSuggestThreads(ulong xsize, ulong ysize);

    /// <summary>
    /// Destroys the runner created by <see cref="JxlResizableParallelRunnerCreate"/>.
    /// </summary>
    /// <param name="runner_opaque">The runner to destroy.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlResizableParallelRunnerDestroy(JxlResizableParallelRunnerPtr runner_opaque);


    /// <summary>
    /// Get function pointer of <see cref="JxlResizableParallelRunner"/>.
    /// </summary>
    /// <returns>Function pointer</returns>
    public static IntPtr GetJxlResizableParallelRunner()
    {
        return NativeLibrary.GetExport(NativeLibrary.Load(LibraryName), nameof(JxlResizableParallelRunner));
    }


    /// <summary>
    /// <para>
    /// Parallel run initialization callback. See <see cref="JxlParallelRunner"/> for details.
    /// </para>
    /// <para>
    /// This function MUST be called by the <see cref="JxlParallelRunner"/> only once, on the
    /// same thread that called <see cref="JxlParallelRunner"/>, before any parallel
    /// execution. The purpose of this call is to provide the maximum number of
    /// threads that the <see cref="JxlParallelRunner"/> will use, which can be used by JPEG XL to allocate
    /// per-thread storage if needed.
    /// </para>
    /// </summary>
    /// <param name="jpegxl_opaque">the <c>jpegxl_opaque</c> handle provided to <see cref="JxlParallelRunner"/> must be passed here.</param>
    /// <param name="num_threads">the maximum number of threads. This value must be positive.</param>
    /// <returns>0 if the initialization process was successful. an error code if there was an error, which should be returned by <see cref="JxlParallelRunner"/>.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate JxlParallelRetCode JxlParallelRunInit(void* jpegxl_opaque, nuint num_threads);


    /// <summary>
    /// <para>
    /// Parallel run data processing callback. See <see cref="JxlParallelRunner"/> for details.
    /// </para>
    /// <para>
    /// This function MUST be called once for every number in the range [<c>start_range</c>,
    /// <c>end_range</c>) (including <c>start_range</c> but not including <c>end_range</c>) passing this
    /// number as the value. Calls for different value may be executed from
    /// different threads in parallel.
    /// </para>
    /// </summary>
    /// <param name="jpegxl_opaque">the <c>jpegxl_opaque</c> handle provided to
    /// <see cref="JxlParallelRunner"/> must be passed here.</param>
    /// <param name="value">the number in the range [<c>start_range</c>, <c>end_range</c>) of the call.</param>
    /// <param name="thread_id">the thread number where this function is being called from.
    /// This must be lower than the <c>num_threads</c> value passed to <see cref="JxlParallelRunInit"/>.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void JxlParallelRunFunction(void* jpegxl_opaque, uint value, nuint thread_id);


    /// <summary>
    /// <para>
    /// JxlParallelRunner function type. A parallel runner implementation can be
    /// provided by a JPEG XL caller to allow running computations in multiple
    /// threads. This function must call the initialization function init in the
    /// same thread that called it and then call the passed func once for every
    /// number in the range [<c>start_range</c>, <c>end_range</c>) (including <c>start_range</c> but not
    /// including <c>end_range</c>) possibly from different multiple threads in parallel.
    /// </para>
    /// <para>
    /// The <see cref="JxlParallelRunner"/> function does not need to be re-entrant. This
    /// means that the same <see cref="JxlParallelRunner"/> function with the same
    /// <c>runner_opaque</c> provided parameter will not be called from the library from
    /// either <paramref name="init"/> or <paramref name="func"/> in the same decoder or encoder instance. However, a single decoding
    /// or encoding instance may call the provided <see cref="JxlParallelRunner"/> multiple
    /// times for different parts of the decoding or encoding process.
    /// </para>
    /// </summary>
    /// <param name="runner_opaque">opaque data provided by the caller for the parallel runner</param>
    /// <param name="jpegxl_opaque">opaque data from JPEG XL library</param>
    /// <param name="init"><see cref="JxlParallelRunInit"/> initialization callback that must be called once before parallel execution</param>
    /// <param name="func"><see cref="JxlParallelRunFunction"/> function to be called for each value in the range</param>
    /// <param name="start_range">start of the range (inclusive)</param>
    /// <param name="end_range">end of the range (exclusive)</param>
    /// <returns>
    /// <para>
    /// 0 if the init call succeeded (returned 0) and no other error
    /// occurred in the runner code.
    /// </para>
    /// <para>
    /// <see cref="JxlParallelRetCode.Error"/> if an error occurred in the runner
    /// code, for example, setting up the threads.
    /// </para>
    /// <para>
    /// the return value of <paramref name="init"/> if non-zero.
    /// </para>
    /// </returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate JxlParallelRetCode JxlParallelRunner(void* runner_opaque, void* jpegxl_opaque, delegate* unmanaged[Cdecl]<void*, nuint, JxlParallelRetCode> init, delegate* unmanaged[Cdecl]<void*, uint, nuint, void> func, uint start_range, uint end_range);

}
