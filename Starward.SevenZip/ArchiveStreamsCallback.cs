namespace Starward.SevenZip
{
    public class FileProgressProperty
    {
        public ulong StartRead { get; set; }
        public ulong EndRead { get; set; }
        public int Count { get; set; }
    }
    public class FileStatusProperty
    {
        public string Name { get; set; }
    }

    internal class ArchiveStreamsCallback : IArchiveExtractCallback
    {
        private readonly IList<CancellableFileStream> streams;

        public event EventHandler<FileProgressProperty> ReadProgress;
        public event EventHandler<FileStatusProperty> ReadStatus;
        private void UpdateProgress(FileProgressProperty e) => ReadProgress?.Invoke(this, e);
        private void UpdateStatus(FileStatusProperty e) => ReadStatus?.Invoke(this, e);

        private ulong TotalSize = 0;
        private ulong TotalRead = 0;
        private string CurrentName = "";
        private int Count = 0;

        public ArchiveStreamsCallback(IList<CancellableFileStream> streams)
        {
            this.streams = streams;
        }

        public void SetTotal(ulong total)
        {
            TotalSize = total;
            UpdateProgress(new FileProgressProperty { StartRead = TotalRead, EndRead = TotalSize, Count = Count });
        }

        public void SetCompleted(ref ulong completeValue)
        {
            TotalRead = completeValue;
            UpdateProgress(new FileProgressProperty { StartRead = TotalRead, EndRead = TotalSize, Count = Count });
        }

        public int GetStream(uint index, out ISequentialOutStream outStream, AskMode askExtractMode)
        {
            if (askExtractMode != AskMode.kExtract)
            {
                outStream = null;
                return 0;
            }

            if (streams == null)
            {
                outStream = null;
                return 0;
            }

            CancellableFileStream stream = streams[(int)index];

            if (stream == null)
            {
                outStream = null;
                return 0;
            }
            else
            {
                CurrentName = stream.Name;
                Count++;
                UpdateStatus(new FileStatusProperty { Name = CurrentName });
            }

            outStream = new OutStreamWrapper(stream);

            return 0;
        }

        public void PrepareOperation(AskMode askExtractMode)
        {
        }

        public void SetOperationResult(OperationResult resultEOperationResult)
        {
        }
    }
}