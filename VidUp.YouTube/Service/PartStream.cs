using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drexel.VidUp.Youtube.Service
{
    public class PartStream : Stream
    {
        private Stream stream;
        private long length;
        private long bytesRead = 0;

        public PartStream(Stream stream, int length)
        {
            this.stream = stream;
            this.length = length;

            long remainingLength = this.stream.Length - this.stream.Position;
            if (remainingLength < this.length)
            {
                this.length = remainingLength;
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.bytesRead >= this.length)
            {
                return 0;
            }

            if (this.bytesRead + count > this.length)
            {
                count = (int)(this.length - this.bytesRead);
            }

            int currentBytesRead = this.stream.Read(buffer, 0, count);
            this.bytesRead += currentBytesRead;
            return currentBytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead { get => true; }
        public override bool CanSeek { get => false; }
        public override bool CanWrite { get => false; }
        public override long Length { get => this.length; }

        public override long Position
        {
            get
            {
                return this.bytesRead;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Close()
        {
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
