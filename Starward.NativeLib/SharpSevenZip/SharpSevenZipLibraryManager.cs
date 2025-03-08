using SharpSevenZip.Exceptions;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

namespace SharpSevenZip;

/// <summary>
/// 7-zip library low-level wrapper.
/// </summary>
internal static class SharpSevenZipLibraryManager
{
    /// <summary>
    /// Synchronization root for all locking.
    /// </summary>
    private static readonly Lock SyncRoot = new();

    /// <summary>
    /// Path to the 7-zip dll.
    /// </summary>
    /// <remarks>7zxa.dll supports only decoding from .7z archives.
    /// Features of 7za.dll: 
    ///     - Supporting 7z format;
    ///     - Built encoders: LZMA, PPMD, BCJ, BCJ2, COPY, AES-256 Encryption.
    ///     - Built decoders: LZMA, PPMD, BCJ, BCJ2, COPY, AES-256 Encryption, BZip2, Deflate.
    /// 7z.dll (from the 7-zip distribution) supports every InArchiveFormat for encoding and decoding.
    /// </remarks>
    private static string? _libraryFileName;

    private static string? DetermineLibraryFilePath()
    {
        string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLower();
        string location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string libraryFilePath;
        if (File.Exists(libraryFilePath = Path.Combine(location, "7z.dll")))
        {
            return libraryFilePath;
        }
        else if (File.Exists(libraryFilePath = Path.Combine(location, arch, "7z.dll")))
        {
            return libraryFilePath;
        }
        else if (File.Exists(libraryFilePath = Path.Combine(location, $"7z-{arch}.dll")))
        {
            return libraryFilePath;
        }
        else if (File.Exists(libraryFilePath = Path.Combine(location, "bin", arch, "7z.dll")))
        {
            return libraryFilePath;
        }
        else if (File.Exists(libraryFilePath = Path.Combine(location, "bin", $"7z-{arch}.dll")))
        {
            return libraryFilePath;
        }
        else if (File.Exists(libraryFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.dll")))
        {
            return libraryFilePath;
        }
        else
        {
            throw new SharpSevenZipLibraryException("DLL file does not exist.");
        }
    }

    /// <summary>
    /// 7-zip library handle.
    /// </summary>
    private static IntPtr _modulePtr;

#if ENABLE_LIBRARY_FEATURES
    /// <summary>
    /// 7-zip library features.
    /// </summary>
    private static LibraryFeature? _features;
#endif

    private static Dictionary<object, Dictionary<InArchiveFormat, IInArchive?>>? _inArchives;
    private static Dictionary<object, Dictionary<OutArchiveFormat, IOutArchive?>>? _outArchives;
    private static int _totalUsers;
    private static bool? _modifyCapable;

    private static void InitUserInFormat(object user, InArchiveFormat format)
    {
        if (!_inArchives!.TryGetValue(user, out Dictionary<InArchiveFormat, IInArchive?>? value))
        {
            value = new Dictionary<InArchiveFormat, IInArchive?>();
            _inArchives.Add(user, value);
        }

        if (!value.ContainsKey(format))
        {
            value.Add(format, null);
            _totalUsers++;
        }
    }

    private static void InitUserOutFormat(object user, OutArchiveFormat format)
    {
        if (!_outArchives!.TryGetValue(user, out Dictionary<OutArchiveFormat, IOutArchive?>? value))
        {
            value = new Dictionary<OutArchiveFormat, IOutArchive?>();
            _outArchives.Add(user, value);
        }

        if (!value.ContainsKey(format))
        {
            value.Add(format, null);
            _totalUsers++;
        }
    }

    private static void Init()
    {
        _inArchives = new Dictionary<object, Dictionary<InArchiveFormat, IInArchive?>>();
        _outArchives = new Dictionary<object, Dictionary<OutArchiveFormat, IOutArchive?>>();
    }

    /// <summary>
    /// Loads the 7-zip library if necessary and adds user to the reference list
    /// </summary>
    /// <param name="user">Caller of the function</param>
    /// <param name="format">Archive format</param>
    public static void LoadLibrary(object user, Enum format)
    {
        lock (SyncRoot)
        {
            if (_inArchives == null || _outArchives == null)
            {
                Init();
            }

            if (_modulePtr == IntPtr.Zero)
            {
                _libraryFileName ??= DetermineLibraryFilePath();

                if (!File.Exists(_libraryFileName))
                {
                    throw new SharpSevenZipLibraryException("DLL file does not exist.");
                }

                if ((_modulePtr = NativeMethods.LoadLibrary(_libraryFileName!)) == IntPtr.Zero)
                {
                    throw new SharpSevenZipLibraryException($"failed to load library from \"{_libraryFileName}\".");
                }

                if (NativeMethods.GetProcAddress(_modulePtr, "GetHandlerProperty") == IntPtr.Zero)
                {
                    NativeMethods.FreeLibrary(_modulePtr);
                    throw new SharpSevenZipLibraryException("library is invalid.");
                }
            }

            if (format is InArchiveFormat archiveFormat)
            {
                InitUserInFormat(user, archiveFormat);
                return;
            }

            if (format is OutArchiveFormat outArchiveFormat)
            {
                InitUserOutFormat(user, outArchiveFormat);
                return;
            }

            throw new ArgumentException($"Enum {format} is not a valid archive format attribute!");
        }
    }

    /// <summary>
    /// Gets the value indicating whether the library supports modifying archives.
    /// </summary>
    public static bool ModifyCapable
    {
        get
        {
            lock (SyncRoot)
            {
                if (!_modifyCapable.HasValue)
                {
                    _libraryFileName ??= DetermineLibraryFilePath();

                    var dllVersionInfo = FileVersionInfo.GetVersionInfo(_libraryFileName!);
                    _modifyCapable = dllVersionInfo.FileMajorPart >= 9;
                }

                return _modifyCapable.Value;
            }
        }
    }

#if ENABLE_LIBRARY_FEATURES

    static readonly string Namespace = Assembly.GetExecutingAssembly().GetManifestResourceNames()[0].Split('.')[0];

    private static string GetResourceString(string str)
    {
        return Namespace + ".arch." + str;
    }

    private static bool ExtractionBenchmark(string archiveFileName, Stream outStream, ref LibraryFeature? features, LibraryFeature testedFeature)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetResourceString(archiveFileName));

        try
        {
            using var extractor = new SharpSevenZipExtractor(stream!);
            extractor.ExtractFile(0, outStream);
        }
        catch (Exception)
        {
            return false;
        }

        features |= testedFeature;
        return true;
    }

    private static bool CompressionBenchmark(Stream inStream, Stream outStream, OutArchiveFormat format, CompressionMethod method, ref LibraryFeature? features, LibraryFeature testedFeature)
    {
        try
        {
            var compressor = new SharpSevenZipCompressor
            {
                ArchiveFormat = format,
                CompressionMethod = method
            };

            compressor.CompressStream(inStream, outStream);
        }
        catch (Exception)
        {
            return false;
        }

        features |= testedFeature;
        return true;
    }
#endif


    public static LibraryFeature CurrentLibraryFeatures
    {
        get
        {
            throw new NotImplementedException();
#if ENABLE_LIBRARY_FEATURES
            lock (SyncRoot)
            {
                if (_features.HasValue)
                {
                    return _features.Value;
                }

                _features = LibraryFeature.None;

                #region Benchmark

                #region Extraction features

                using (var outStream = new MemoryStream())
                {
                    ExtractionBenchmark("Test.lzma.7z", outStream, ref _features, LibraryFeature.Extract7z);
                    ExtractionBenchmark("Test.lzma2.7z", outStream, ref _features, LibraryFeature.Extract7zLZMA2);

                    var i = 0;

                    if (ExtractionBenchmark("Test.bzip2.7z", outStream, ref _features, _features!.Value))
                    {
                        i++;
                    }

                    if (ExtractionBenchmark("Test.ppmd.7z", outStream, ref _features, _features!.Value))
                    {
                        i++;
                        if (i == 2 && (_features & LibraryFeature.Extract7z) != 0 &&
                            (_features & LibraryFeature.Extract7zLZMA2) != 0)
                        {
                            _features |= LibraryFeature.Extract7zAll;
                        }
                    }

                    ExtractionBenchmark("Test.rar", outStream, ref _features, LibraryFeature.ExtractRar);
                    ExtractionBenchmark("Test.tar", outStream, ref _features, LibraryFeature.ExtractTar);
                    ExtractionBenchmark("Test.txt.bz2", outStream, ref _features, LibraryFeature.ExtractBzip2);
                    ExtractionBenchmark("Test.txt.gz", outStream, ref _features, LibraryFeature.ExtractGzip);
                    ExtractionBenchmark("Test.txt.xz", outStream, ref _features, LibraryFeature.ExtractXz);
                    ExtractionBenchmark("Test.zip", outStream, ref _features, LibraryFeature.ExtractZip);
                }

                #endregion

                #region Compression features

                using (var inStream = new MemoryStream())
                {
                    inStream.Write(Encoding.UTF8.GetBytes("Test"), 0, 4);

                    using var outStream = new MemoryStream();

                    CompressionBenchmark(inStream, outStream,
                        OutArchiveFormat.SevenZip, CompressionMethod.Lzma,
                        ref _features, LibraryFeature.Compress7z);
                    CompressionBenchmark(inStream, outStream,
                        OutArchiveFormat.SevenZip, CompressionMethod.Lzma2,
                        ref _features, LibraryFeature.Compress7zLZMA2);

                    var i = 0;

                    if (_features != null && CompressionBenchmark(inStream, outStream,
                            OutArchiveFormat.SevenZip, CompressionMethod.BZip2,
                            ref _features, _features.Value))
                    {
                        i++;
                    }

                    if (_features != null && CompressionBenchmark(inStream, outStream,
                            OutArchiveFormat.SevenZip, CompressionMethod.Ppmd,
                            ref _features, _features.Value))
                    {
                        i++;
                        if (i == 2 && (_features & LibraryFeature.Compress7z) != 0 &&
                        (_features & LibraryFeature.Compress7zLZMA2) != 0)
                        {
                            _features |= LibraryFeature.Compress7zAll;
                        }
                    }

                    CompressionBenchmark(inStream, outStream,
                        OutArchiveFormat.Zip, CompressionMethod.Default,
                        ref _features, LibraryFeature.CompressZip);
                    CompressionBenchmark(inStream, outStream,
                        OutArchiveFormat.BZip2, CompressionMethod.Default,
                        ref _features, LibraryFeature.CompressBzip2);
                    CompressionBenchmark(inStream, outStream,
                        OutArchiveFormat.GZip, CompressionMethod.Default,
                        ref _features, LibraryFeature.CompressGzip);
                    CompressionBenchmark(inStream, outStream,
                        OutArchiveFormat.Tar, CompressionMethod.Default,
                        ref _features, LibraryFeature.CompressTar);
                    CompressionBenchmark(inStream, outStream,
                        OutArchiveFormat.XZ, CompressionMethod.Default,
                        ref _features, LibraryFeature.CompressXz);
                }

                #endregion

                #endregion

                if (_features != null && ModifyCapable && (_features.Value & LibraryFeature.Compress7z) != 0)
                {
                    _features |= LibraryFeature.Modify;
                }

                return _features!.Value;
            }
#endif
        }
    }

    /// <summary>
    /// Removes user from reference list and frees the 7-zip library if it becomes empty
    /// </summary>
    /// <param name="user">Caller of the function</param>
    /// <param name="format">Archive format</param>
    public static void FreeLibrary(object user, Enum format)
    {
        lock (SyncRoot)
        {
            if (_modulePtr != IntPtr.Zero)
            {
                if (format is InArchiveFormat archiveFormat)
                {
                    if (_inArchives != null && _inArchives.TryGetValue(user, out Dictionary<InArchiveFormat, IInArchive?>? userValue) &&
                        userValue.TryGetValue(archiveFormat, out IInArchive? formatValue) &&
                        formatValue != null)
                    {
                        try
                        {
                            Marshal.ReleaseComObject(formatValue);
                        }
                        catch (InvalidComObjectException) { }

                        userValue.Remove(archiveFormat);
                        _totalUsers--;

                        if (userValue.Count == 0)
                        {
                            _inArchives.Remove(user);
                        }
                    }
                }

                if (format is OutArchiveFormat outArchiveFormat)
                {
                    if (_outArchives != null && _outArchives.TryGetValue(user, out Dictionary<OutArchiveFormat, IOutArchive?>? userValue) &&
                        userValue.TryGetValue(outArchiveFormat, out IOutArchive? formatValue) &&
                        formatValue != null)
                    {
                        try
                        {
                            Marshal.ReleaseComObject(formatValue);
                        }
                        catch (InvalidComObjectException) { }

                        userValue.Remove(outArchiveFormat);
                        _totalUsers--;

                        if (userValue.Count == 0)
                        {
                            _outArchives.Remove(user);
                        }
                    }
                }

                if ((_inArchives == null || _inArchives.Count == 0) && (_outArchives == null || _outArchives.Count == 0))
                {
                    _inArchives = null;
                    _outArchives = null;

                    if (_totalUsers == 0)
                    {
                        //NativeMethods.FreeLibrary(_modulePtr);
                        //_modulePtr = IntPtr.Zero;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets IInArchive interface to extract 7-zip archives.
    /// </summary>
    /// <param name="format">Archive format.</param>
    /// <param name="user">Archive format user.</param>
    public static IInArchive InArchive(InArchiveFormat format, object user)
    {
        lock (SyncRoot)
        {
            if (!_inArchives!.TryGetValue(user, out Dictionary<InArchiveFormat, IInArchive?>? archives) || archives[format] == null)
            {
                if (_modulePtr == IntPtr.Zero)
                {
                    LoadLibrary(user, format);

                    if (_modulePtr == IntPtr.Zero)
                    {
                        throw new SharpSevenZipLibraryException();
                    }
                }

                var createObject = (NativeMethods.CreateObjectDelegate)
                    Marshal.GetDelegateForFunctionPointer(
                        NativeMethods.GetProcAddress(_modulePtr, "CreateObject"),
                        typeof(NativeMethods.CreateObjectDelegate))
                    ?? throw new SharpSevenZipLibraryException();

                object result;
                var interfaceId = typeof(IInArchive).GUID;
                var classId = Formats.InFormatGuids[format];

                try
                {
                    createObject(ref classId, ref interfaceId, out result);
                }
                catch (Exception)
                {
                    throw new SharpSevenZipLibraryException("Your 7-zip library does not support this archive type.");
                }

                InitUserInFormat(user, format);
                _inArchives[user][format] = result as IInArchive;
            }

            return _inArchives[user][format]!;
        }
    }

    /// <summary>
    /// Gets IOutArchive interface to pack 7-zip archives.
    /// </summary>
    /// <param name="format">Archive format.</param>  
    /// <param name="user">Archive format user.</param>
    public static IOutArchive OutArchive(OutArchiveFormat format, object user)
    {
        lock (SyncRoot)
        {
            if (_outArchives![user][format] == null)
            {
                if (_modulePtr == IntPtr.Zero)
                {
                    throw new SharpSevenZipLibraryException();
                }

                var createObject = (NativeMethods.CreateObjectDelegate)
                    Marshal.GetDelegateForFunctionPointer(
                        NativeMethods.GetProcAddress(_modulePtr, "CreateObject"),
                        typeof(NativeMethods.CreateObjectDelegate));

                var interfaceId = typeof(IOutArchive).GUID;

                try
                {
                    var classId = Formats.OutFormatGuids[format];
                    createObject(ref classId, ref interfaceId, out var result);

                    InitUserOutFormat(user, format);
                    _outArchives[user][format] = result as IOutArchive;
                }
                catch (Exception)
                {
                    throw new SharpSevenZipLibraryException("Your 7-zip library does not support this archive type.");
                }
            }

            return _outArchives[user][format]!;
        }
    }

    public static void SetLibraryPath(string libraryPath)
    {
        if (_modulePtr != IntPtr.Zero && !Path.GetFullPath(libraryPath).Equals(Path.GetFullPath(_libraryFileName!), StringComparison.OrdinalIgnoreCase))
        {
            throw new SharpSevenZipLibraryException($"can not change the library path while the library \"{_libraryFileName}\" is being used.");
        }

        if (!File.Exists(libraryPath))
        {
            throw new SharpSevenZipLibraryException($"can not change the library path because the file \"{libraryPath}\" does not exist.");
        }

        _libraryFileName = libraryPath;
#if ENABLE_LIBRARY_FEATURES
        _features = null;
#endif
    }
}
