namespace Snap.HPatch;


#pragma warning disable CS3016 // 作为特性参数的数组不符合 CLS



public static unsafe partial class HPatch
{
    private sealed partial class InputSliceStream : Stream
    {
        private readonly FileHandleInput* input;
        private readonly ulong begin;
        private readonly ulong end;
        private ulong position;

        public InputSliceStream(FileHandleInput* input, ulong begin, ulong end)
        {
            this.input = input;
            this.begin = begin;
            this.end = end;
            position = begin;
        }

        public override bool CanRead { get => true; }

        public override bool CanSeek { get => true; }

        public override bool CanWrite { get => false; }

        public override long Length { get => (long)(end - begin); }

        public override long Position { get => (long)(position - begin); set => position = (ulong)value + begin; }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count > (int)(end - position))
            {
                count = (int)(end - position);
            }

            if (count <= 0)
            {
                return 0;
            }

            fixed (byte* pBuffer = buffer)
            {
                if (input->Read(input, position, pBuffer, pBuffer + count))
                {
                    position += (ulong)count;
                    return count;
                }
            }

            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Seeking is not supported in StreamInputStream.");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Setting length is not supported in StreamInputStream.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Writing is not supported in StreamInputStream.");
        }
    }

}


