using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.Youtube.Service
{
    public class ThrottledBufferedStream : Stream, IDisposable
    {
        private const int historyForUploadInSeconds = 3;
        private const int memoryBufferSizeInBytes = 20 * 1024 * 1024 ; //20 MegaByte
        private const long tickMultiplierForSeconds = 10000000;
        private const int keepHistoryForInSeconds = 30;
        private const int historyForStatsInSeconds = 20;

        private DateTime lastStatsUpdate;
        private static TimeSpan twoSeconds = new TimeSpan(0, 0, 2);
        private Action<YoutubeUploadStats> updateUploadProgress;
        private YoutubeUploadStats stats;
        private Upload upload;

        private Stream baseStream;
        private long maximumBytesPerSecondRead;
        private Dictionary<long, int> bytesPerTick = new Dictionary<long, int>();

        private byte[] memoryBuffer = new byte[ThrottledBufferedStream.memoryBufferSizeInBytes];
        private int bufferPosition = 0;
        private int bytesInBuffer = 0;

        private long position = 0;

        private long currentTicks
        {
            get
            {
                return DateTime.Now.Ticks;
            }
        }

        public int CurrentSpeedInBytesPerSecond
        {
            get
            {
                long historyTicks = currentTicks - ThrottledBufferedStream.historyForStatsInSeconds * ThrottledBufferedStream.tickMultiplierForSeconds;
                var historyBytes = this.bytesPerTick.Where(kvp => kvp.Key > historyTicks).ToList();
                int sum = historyBytes.Sum(historyByte => historyByte.Value);

                long minTick = this.bytesPerTick.Min(kvp => kvp.Key);
                if (minTick > this.currentTicks - ThrottledBufferedStream.historyForStatsInSeconds *
                    ThrottledBufferedStream.tickMultiplierForSeconds)
                {
                    TimeSpan duration = DateTime.Now - new DateTime(minTick);
                    return (int) ((sum / duration.TotalMilliseconds) * 1000);
                }
                else
                {
                    return sum / ThrottledBufferedStream.historyForStatsInSeconds;
                }
            }
        }

        public long MaximumBytesPerSecond
        {
            get
            {
                return this.maximumBytesPerSecondRead;
            }
            set
            {
                if (this.maximumBytesPerSecondRead != value)
                {
                    this.maximumBytesPerSecondRead = value;
                }
            }
        }

        public override bool CanRead
        {
            get
            {
                return this.baseStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return this.baseStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return this.baseStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return this.position;
            }
            set
            {
                this.bufferPosition = 0;
                this.baseStream.Position = value;
                this.position = value;
            }
        }

        public ThrottledBufferedStream(Stream baseStream, long maximumBytesPerSecond, Action<YoutubeUploadStats> updateUploadProgress, YoutubeUploadStats stats, Upload upload)
        {
            if (ThrottledBufferedStream.historyForUploadInSeconds > ThrottledBufferedStream.keepHistoryForInSeconds)
            {
                throw new InvalidOperationException("History not long enough for upload throttling.");
            }

            if (ThrottledBufferedStream.historyForStatsInSeconds > ThrottledBufferedStream.keepHistoryForInSeconds)
            {
                throw new InvalidOperationException("History not long enough for stats.");
            }

            if (baseStream == null)
            {
                throw new ArgumentNullException("baseStream");
            }

            if (maximumBytesPerSecond < 0)
            {
                throw new ArgumentOutOfRangeException("maximumBytesPerSecond",
                    maximumBytesPerSecond, "The maximum number of bytes per second can't be negatie.");
            }

            this.updateUploadProgress = updateUploadProgress;
            this.stats = stats;
            this.upload = upload;
            this.lastStatsUpdate = DateTime.Now;

            this.baseStream = baseStream;
            this.maximumBytesPerSecondRead = maximumBytesPerSecond;
            this.position = this.baseStream.Position;
        }

        public override void Flush()
        {
            this.baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentException("Read would exceed buffer length due to offset and count values.");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", "Offset cannot be negative.");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", "Count cannot be negative.");
            }

            long currentTicks = this.currentTicks;

            this.throttle(currentTicks);

            int bytesRead = this.readInternal(buffer, offset, count);
            this.position += bytesRead;

            if (this.bytesPerTick.ContainsKey(currentTicks))
            {
                this.bytesPerTick[currentTicks] += bytesRead;
            }
            else
            {
                this.bytesPerTick.Add(currentTicks, bytesRead);
            }

            if (DateTime.Now - this.lastStatsUpdate > ThrottledBufferedStream.twoSeconds)
            {
                stats.CurrentSpeedInBytesPerSecond = this.CurrentSpeedInBytesPerSecond;
                upload.BytesSent = this.position;
                updateUploadProgress(stats);
                this.lastStatsUpdate = DateTime.Now;
            }

            return bytesRead;
        }

        private int readInternal(byte[] buffer, int offset, int count)
        {
            if (this.bufferPosition == 0)
            {
                this.bytesInBuffer = this.readNtexToBuffer();
                if (this.bytesInBuffer == 0)
                {
                    return 0;
                }
            }

            int bytesLeft = this.bytesInBuffer - this.bufferPosition;

            if (count <= bytesLeft)
            {
                Array.Copy(this.memoryBuffer, this.bufferPosition, buffer, offset, count);
                this.bufferPosition += count;

                if (count == bytesLeft)
                {
                    this.bufferPosition = 0;
                }

                return count;
            }
            else
            {
                Array.Copy(this.memoryBuffer, this.bufferPosition, buffer, offset, bytesLeft);
                this.bufferPosition = 0;

                count -= bytesLeft;

                //bytesLeft=bytesRead
                return bytesLeft += this.readInternal(buffer, offset + bytesLeft, count);
            }
        }

        private int readNtexToBuffer()
        {
            return this.baseStream.Read(this.memoryBuffer, 0, ThrottledBufferedStream.memoryBufferSizeInBytes);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            this.bufferPosition = 0;
            this.position = this.baseStream.Seek(offset, origin);
            return this.position;
        }

        public override void SetLength(long value)
        {
            this.baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.baseStream.Write(buffer, offset, count);
        }

        public override string ToString()
        {
            return this.baseStream.ToString();
        }

        public override void Close()
        {
            this.baseStream.Close();
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            this.baseStream.Dispose();
            base.Dispose(disposing);
        }



        private void throttle(long currentTicks)
        {
            // Make sure the buffer isn't empty.
            if (this.maximumBytesPerSecondRead <= 0)
            {
                return;
            }

            long historyTicks = currentTicks - ThrottledBufferedStream.historyForUploadInSeconds * ThrottledBufferedStream.tickMultiplierForSeconds;

            var historyBytes = this.bytesPerTick.Where(kvp => kvp.Key > historyTicks);

            foreach (var outdatedEntry in this.bytesPerTick.Where(kvp => kvp.Key < currentTicks - ThrottledBufferedStream.keepHistoryForInSeconds * ThrottledBufferedStream.tickMultiplierForSeconds).ToArray())
            {
                this.bytesPerTick.Remove(outdatedEntry.Key);
            }

            if (historyBytes.Count() > 1)
            {
                long byteCountRead = historyBytes.Sum(kvp => kvp.Value);

                // Calculate the current bps.
                long targetBytesInHistory = this.maximumBytesPerSecondRead * ThrottledBufferedStream.historyForUploadInSeconds;

                // If the bps are more then the maximum bps, try to throttle.
                if (byteCountRead > targetBytesInHistory)
                {
                    // Calculate the time to sleep.
                    long bytesTooMuch = byteCountRead - targetBytesInHistory;
                    float sleep = (float)bytesTooMuch / this.maximumBytesPerSecondRead;
                    int toSleep = (int)(sleep * 1000);

                    if (toSleep > 1)
                    {
                        Thread.Sleep(toSleep);
                    }
                }

            }
        }
    }
}