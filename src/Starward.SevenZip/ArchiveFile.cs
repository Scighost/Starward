using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Starward.SevenZip
{

    [SupportedOSPlatform("windows")]
    public class ArchiveFile : IDisposable
    {
        public class ExtractProgressProp
        {
            public ExtractProgressProp(ulong Read, ulong TotalRead, ulong TotalSize, double TotalSecond, int Count, int TotalCount)
            {
                this.Read = Read;
                this.TotalRead = TotalRead;
                this.TotalSize = TotalSize;
                Speed = (ulong)(TotalRead / TotalSecond);
                this.Count = Count;
                this.TotalCount = TotalCount;
            }
            public int Count { get; set; }
            public int TotalCount { get; set; }
            public ulong Read { get; private set; }
            public ulong TotalRead { get; private set; }
            public ulong TotalSize { get; private set; }
            public ulong Speed { get; private set; }
            public double PercentProgress => TotalRead / (double)TotalSize * 100;
            public TimeSpan TimeLeft => TimeSpan.FromSeconds((TotalSize - TotalRead) / Speed);
        }

        private SevenZipHandle sevenZipHandle;
        private readonly IInArchive archive;
        private readonly InStreamWrapper archiveStream;
        private IList<Entry> entries;
        private int TotalCount;
        private CultureInfo culture = CultureInfo.CurrentCulture;

        private string libraryFilePath;
        public event EventHandler<ExtractProgressProp> ExtractProgress;
        public void UpdateProgress(ExtractProgressProp e) => ExtractProgress?.Invoke(this, e);

        public ArchiveFile(string archiveFilePath, string libraryFilePath = null)
        {

            this.libraryFilePath = libraryFilePath;

            InitializeAndValidateLibrary();

            if (!File.Exists(archiveFilePath))
            {
                throw new SevenZipException("Archive file not found");
            }

            SevenZipFormat format;
            string extension = Path.GetExtension(archiveFilePath);

            if (GuessFormatFromExtension(extension, out format))
            {
                // great
            }
            else if (GuessFormatFromSignature(archiveFilePath, out format))
            {
                // success
            }
            else
            {
                throw new SevenZipException(Path.GetFileName(archiveFilePath) + " is not a known archive type");
            }

            archive = sevenZipHandle.CreateInArchive(Formats.FormatGuidMapping[format]);
            archiveStream = new InStreamWrapper(File.OpenRead(archiveFilePath));
        }

        public ArchiveFile(Stream archiveStream, SevenZipFormat? format = null, string libraryFilePath = null)
        {
            this.libraryFilePath = libraryFilePath;

            InitializeAndValidateLibrary();

            if (archiveStream == null)
            {
                throw new SevenZipException("archiveStream is null");
            }

            if (format == null)
            {
                SevenZipFormat guessedFormat;

                if (GuessFormatFromSignature(archiveStream, out guessedFormat))
                {
                    format = guessedFormat;
                }
                else
                {
                    throw new SevenZipException("Unable to guess format automatically");
                }
            }

            archive = sevenZipHandle.CreateInArchive(Formats.FormatGuidMapping[format.Value]);
            this.archiveStream = new InStreamWrapper(archiveStream);
            TotalCount = Entries.Sum(x => x.IsFolder ? 0 : 1);
        }

        public void Extract(string outputFolder, bool overwrite = false)
        {
            Extract(entry =>
            {
                string fileName = Path.Combine(outputFolder, entry.FileName);

                if (entry.IsFolder)
                {
                    return fileName;
                }

                if (!File.Exists(fileName) || overwrite)
                {
                    return fileName;
                }

                return null;
            });
        }

        public void Extract(Func<Entry, string> getOutputPath, CancellationToken Token = new CancellationToken())
        {
            IList<CancellableFileStream> fileStreams = new List<CancellableFileStream>();
            ArchiveStreamsCallback streamCallback;

            try
            {
                foreach (Entry entry in Entries)
                {
                    string outputPath = getOutputPath(entry);

                    if (outputPath == null) // getOutputPath = null means SKIP
                    {
                        fileStreams.Add(null);
                        continue;
                    }

                    if (entry.IsFolder)
                    {
                        Directory.CreateDirectory(outputPath);
                        fileStreams.Add(null);
                        continue;
                    }

                    string directoryName = Path.GetDirectoryName(outputPath);

                    if (!string.IsNullOrWhiteSpace(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    fileStreams.Add(new CancellableFileStream(File.Create(outputPath), Token));
                }

                streamCallback = new ArchiveStreamsCallback(fileStreams);
                ExtractProgressStopwatch = Stopwatch.StartNew();
                streamCallback.ReadProgress += StreamCallback_ReadProperty;

                archive.Extract(null, 0xFFFFFFFF, 0, streamCallback);
                streamCallback.ReadProgress -= StreamCallback_ReadProperty;
            }
            finally
            {
                foreach (CancellableFileStream stream in fileStreams)
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                }
            }
        }

        ulong LastSize = 0;

        private ulong GetLastSize(ulong input)
        {
            if (LastSize > input)
                LastSize = input;

            ulong a = input - LastSize;
            LastSize = input;
            return a;
        }

        Stopwatch ExtractProgressStopwatch = Stopwatch.StartNew();
        private void StreamCallback_ReadProperty(object sender, FileProgressProperty e)
        {
            UpdateProgress(new ExtractProgressProp(GetLastSize(e.StartRead),
                e.StartRead, e.EndRead, ExtractProgressStopwatch.Elapsed.TotalSeconds, e.Count, TotalCount));
        }

        public IList<Entry> Entries
        {
            get
            {
                if (entries != null)
                {
                    return entries;
                }

                ulong checkPos = 32 * 1024;
                int open = archive.Open(archiveStream, ref checkPos, null);

                if (open != 0)
                {
                    throw new SevenZipException("Unable to open archive");
                }

                uint itemsCount = archive.GetNumberOfItems();

                entries = new List<Entry>();

                for (uint fileIndex = 0; fileIndex < itemsCount; fileIndex++)
                {
                    string fileName = GetProperty<string>(fileIndex, ItemPropId.kpidPath);
                    bool isFolder = GetProperty<bool>(fileIndex, ItemPropId.kpidIsFolder);
                    bool isEncrypted = GetProperty<bool>(fileIndex, ItemPropId.kpidEncrypted);
                    ulong size = GetProperty<ulong>(fileIndex, ItemPropId.kpidSize);
                    ulong packedSize = GetProperty<ulong>(fileIndex, ItemPropId.kpidPackedSize);
                    DateTime creationTime = GetDateTime(fileIndex, ItemPropId.kpidCreationTime);
                    DateTime lastWriteTime = GetDateTime(fileIndex, ItemPropId.kpidLastWriteTime);
                    DateTime lastAccessTime = GetDateTime(fileIndex, ItemPropId.kpidLastAccessTime);
                    uint crc = GetPropertySafe<uint>(fileIndex, ItemPropId.kpidCRC);
                    uint attributes = GetPropertySafe<uint>(fileIndex, ItemPropId.kpidAttributes);
                    string comment = GetPropertySafe<string>(fileIndex, ItemPropId.kpidComment);
                    string hostOS = GetPropertySafe<string>(fileIndex, ItemPropId.kpidHostOS);
                    string method = GetPropertySafe<string>(fileIndex, ItemPropId.kpidMethod);

                    bool isSplitBefore = GetPropertySafe<bool>(fileIndex, ItemPropId.kpidSplitBefore);
                    bool isSplitAfter = GetPropertySafe<bool>(fileIndex, ItemPropId.kpidSplitAfter);

                    entries.Add(new Entry(archive, fileIndex)
                    {
                        FileName = fileName,
                        IsFolder = isFolder,
                        IsEncrypted = isEncrypted,
                        Size = size,
                        PackedSize = packedSize,
                        CreationTime = creationTime,
                        LastWriteTime = lastWriteTime,
                        LastAccessTime = lastAccessTime,
                        CRC = crc,
                        Attributes = attributes,
                        Comment = comment,
                        HostOS = hostOS,
                        Method = method,
                        IsSplitBefore = isSplitBefore,
                        IsSplitAfter = isSplitAfter
                    });
                }

                return entries;
            }
        }

        private T GetPropertySafe<T>(uint fileIndex, ItemPropId name)
        {
            try
            {
                return GetProperty<T>(fileIndex, name);
            }
            catch (InvalidCastException)
            {
                return default;
            }
        }

        private DateTime GetDateTime(uint fileIndex, ItemPropId name)
        {
            PropVariant propVariant = new PropVariant();
            archive.GetProperty(fileIndex, name, ref propVariant);

            return DateTime.FromFileTime(propVariant.longValue);
        }

        private T GetProperty<T>(uint fileIndex, ItemPropId name)
        {
            PropVariant propVariant = new PropVariant();
            archive.GetProperty(fileIndex, name, ref propVariant);
            object value = propVariant.GetObject();

            if (propVariant.VarType == VarEnum.VT_EMPTY)
            {
                propVariant.Clear();
                return default;
            }

            propVariant.Clear();

            if (value == null)
            {
                return default;
            }

            Type type = typeof(T);
            bool isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            Type underlyingType = isNullable ? Nullable.GetUnderlyingType(type) : type;

            string valueString = value.ToString();
            T result = (T)Convert.ChangeType(valueString, underlyingType);

            return result;
        }

        private void InitializeAndValidateLibrary()
        {
            if (string.IsNullOrWhiteSpace(libraryFilePath))
            {
                if (File.Exists(Path.Combine(AppContext.BaseDirectory, "7z.dll")))
                {
                    libraryFilePath = Path.Combine(AppContext.BaseDirectory, "7z.dll");
                }
            }

            if (string.IsNullOrWhiteSpace(libraryFilePath))
            {
                throw new SevenZipException("libraryFilePath not set");
            }

            if (!File.Exists(libraryFilePath))
            {
                throw new SevenZipException("7z.dll not found");
            }

            try
            {
                sevenZipHandle = new SevenZipHandle(libraryFilePath);
            }
            catch (Exception e)
            {
                throw new SevenZipException("Unable to initialize SevenZipHandle", e);
            }
        }

        private bool GuessFormatFromExtension(string fileExtension, out SevenZipFormat format)
        {
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                format = SevenZipFormat.Undefined;
                return false;
            }

            fileExtension = fileExtension.TrimStart('.').Trim().ToLowerInvariant();

            if (fileExtension.Equals("rar"))
            {
                // 7z has different GUID for Pre-RAR5 and RAR5, but they have both same extension (.rar)
                // If it is [0x52 0x61 0x72 0x21 0x1A 0x07 0x01 0x00] then file is RAR5 otherwise RAR.
                // https://www.rarlab.com/technote.htm

                // We are unable to guess right format just by looking at extension and have to check signature

                format = SevenZipFormat.Undefined;
                return false;
            }

            if (!Formats.ExtensionFormatMapping.ContainsKey(fileExtension))
            {
                format = SevenZipFormat.Undefined;
                return false;
            }

            format = Formats.ExtensionFormatMapping[fileExtension];
            return true;
        }


        private bool GuessFormatFromSignature(string filePath, out SevenZipFormat format)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return GuessFormatFromSignature(fileStream, out format);
            }
        }

        private bool GuessFormatFromSignature(Stream stream, out SevenZipFormat format)
        {
            int longestSignature = Formats.FileSignatures.Values.OrderByDescending(v => v.Length).First().Length;

            byte[] archiveFileSignature = new byte[longestSignature];
            int bytesRead = stream.Read(archiveFileSignature, 0, longestSignature);

            stream.Position -= bytesRead; // go back o beginning

            if (bytesRead != longestSignature)
            {
                format = SevenZipFormat.Undefined;
                return false;
            }

            foreach (KeyValuePair<SevenZipFormat, byte[]> pair in Formats.FileSignatures)
            {
                if (archiveFileSignature.Take(pair.Value.Length).SequenceEqual(pair.Value))
                {
                    format = pair.Key;
                    return true;
                }
            }

            format = SevenZipFormat.Undefined;
            return false;
        }

        ~ArchiveFile()
        {
            Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if (archiveStream != null)
            {
                archiveStream.Dispose();
            }

            if (archive != null)
            {
                Marshal.ReleaseComObject(archive);
            }

            if (sevenZipHandle != null)
            {
                sevenZipHandle.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
