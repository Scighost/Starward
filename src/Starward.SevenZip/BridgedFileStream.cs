namespace Starward.SevenZip
{
    internal class CancellableFileStream : Stream
    {
        private FileStream targetStream;
        private CancellationToken cancelToken;

        internal CancellableFileStream(FileStream targetStream, CancellationToken token = new CancellationToken())
        {
            this.targetStream = targetStream;
            cancelToken = token;
        }

        protected override void Dispose(bool disposing)
        {
            targetStream.Dispose();
            base.Dispose(disposing);
        }

        public override void CopyTo(Stream destination, int bufferSize)
        {
            cancelToken.ThrowIfCancellationRequested();
            base.CopyTo(destination, bufferSize);
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            cancelToken.ThrowIfCancellationRequested();
            targetStream.Write(buffer, offset, count);
        }

        public string Name { get { return targetStream.Name; } }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override long Length
        {
            get { return targetStream.Length; }
        }

        public override long Position
        {
            get { return targetStream.Position; }
            set { targetStream.Position = value; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override void Flush()
        {
            targetStream?.Flush();
        }
    }
}
