using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Drexel.VidUp.Youtube.VideoUploadService
{
    public class ThrottledBufferedStream : Stream
    {
        private const int tickDivider = 312500;
        private const int historyForUploadInSeconds = 3;
        //32 slots per second to keep the slot count stable and reduced independent from upload speeds
        private const int timeSlotMultiplierForSeconds = 10000000 / ThrottledBufferedStream.tickDivider;
        private const int keepHistoryForInSeconds = 15;
        private const int historyForStatsInSeconds = 10;

        private int readCapacity = 100 * 1024 * 1024;
        private BlockingCollection<byte[]> memoryBuffer = new BlockingCollection<byte[]>(1);
        private int memoryBufferSize = 50 * 1024 * 1024;

        private int readBuffer;

        private byte[] currentData;
        private int currentDataPosition;

        private Stream baseStream;
        private long position;

        private long maximumBytesPerSecondRead;
        private Dictionary<long, int> bytesPerTimeSlot = new Dictionary<long, int>();

        private long currentTimeSlot
        {
            get
            {
                return DateTime.Now.Ticks / ThrottledBufferedStream.tickDivider; //divides second in 32 parts
            }
        }

        public int ReadBuffer
        {
            get => this.readBuffer;
        }

        public int CurrentSpeedInBytesPerSecond
        {
            get
            {
                if (this.bytesPerTimeSlot.Count <= 0)
                {
                    return 0;
                }

                long historyTicks = this.currentTimeSlot - ThrottledBufferedStream.historyForStatsInSeconds * ThrottledBufferedStream.timeSlotMultiplierForSeconds;
                KeyValuePair<long, int>[] historyBytes;
                long minTimeSlot;
                lock (this.bytesPerTimeSlot)
                {
                    historyBytes = this.bytesPerTimeSlot.Where(kvp => kvp.Key > historyTicks).ToArray();
                    minTimeSlot = this.bytesPerTimeSlot.Min(kvp => kvp.Key);
                }

                int sum = historyBytes.Sum(historyByte => historyByte.Value);


                if (minTimeSlot > this.currentTimeSlot - ThrottledBufferedStream.historyForStatsInSeconds * ThrottledBufferedStream.timeSlotMultiplierForSeconds)
                {
                    TimeSpan duration = DateTime.Now - new DateTime(minTimeSlot * ThrottledBufferedStream.tickDivider);
                    return (int)((sum / duration.TotalMilliseconds) * 1000);
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
                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException("maximumBytesPerSecond", value, "The maximum number of bytes per second can't be negative.");
                    }

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
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return this.baseStream.Length;
            }
        }

        public int LeReadBufferngth
        {
            get
            {
                return this.readBuffer;
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
                throw new NotSupportedException("Seeking not supported");
            }
        }

        public ThrottledBufferedStream(string filePath, long position, long maximumBytesPerSecond)
        {
            if (ThrottledBufferedStream.historyForUploadInSeconds > ThrottledBufferedStream.keepHistoryForInSeconds)
            {
                throw new InvalidOperationException("History not long enough for upload throttling.");
            }

            if (ThrottledBufferedStream.historyForStatsInSeconds > ThrottledBufferedStream.keepHistoryForInSeconds)
            {
                throw new InvalidOperationException("History not long enough for stats.");
            }

            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            if (maximumBytesPerSecond < 0)
            {
                throw new ArgumentOutOfRangeException("maximumBytesPerSecond", maximumBytesPerSecond, "The maximum number of bytes per second can't be negative.");
            }

            this.baseStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            this.baseStream.Position = position;
            this.position = position;
            this.maximumBytesPerSecondRead = maximumBytesPerSecond;

            Task.Run(this.fillMemoryBuffer);
        }

        public override void Flush()
        {
            this.baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", "Count cannot be negative.");
            }

            if (count == 0)
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

            if (readBuffer <= 0)
            {
                this.readBuffer = buffer.Length;
            }

            long currentTimeSlotInternal = this.currentTimeSlot;

            this.throttle(currentTimeSlotInternal);
            this.cleanHistory(currentTimeSlotInternal);

            int bytesRead = this.readInternal(buffer, offset, count);
            this.position += bytesRead;

            lock (this.bytesPerTimeSlot)
            {
                if (this.bytesPerTimeSlot.ContainsKey(currentTimeSlotInternal))
                {
                    this.bytesPerTimeSlot[currentTimeSlotInternal] += bytesRead;
                }
                else
                {
                    this.bytesPerTimeSlot.Add(currentTimeSlotInternal, bytesRead);
                }
            }

            return bytesRead;
        }

        private void cleanHistory(long currentTimeSlotInternal)
        {
            KeyValuePair<long, int>[] outdatedEntries = this.bytesPerTimeSlot.Where(kvp => kvp.Key < currentTimeSlotInternal - ThrottledBufferedStream.keepHistoryForInSeconds * ThrottledBufferedStream.timeSlotMultiplierForSeconds).ToArray();
            lock (this.bytesPerTimeSlot)
            {
                foreach (var outdatedEntry in outdatedEntries)
                {
                    this.bytesPerTimeSlot.Remove(outdatedEntry.Key);
                }
            }
        }

        private void fillMemoryBuffer()
        {
            while (true)
            {
                byte[] buffer = new byte[this.readCapacity];
                int bytesRead = this.baseStream.Read(buffer, 0, this.readCapacity);
                if (bytesRead == 0)
                {
                    this.memoryBuffer.CompleteAdding();
                    return;
                }

                if (bytesRead < this.readCapacity)
                {
                    byte[] temp = new byte[bytesRead];
                    Array.Copy(buffer, 0, temp, 0, bytesRead);
                    buffer = temp;
                }


                int bufferPosition = 0;
                while (bufferPosition < buffer.Length)
                {
                    int bytesToRead = this.memoryBufferSize;
                    if (bufferPosition + this.memoryBufferSize > buffer.Length)
                    {
                        bytesToRead = buffer.Length - bufferPosition;
                    }

                    byte[] temp = new byte[bytesToRead];
                    Array.Copy(buffer, bufferPosition, temp, 0, bytesToRead);
                    bufferPosition += bytesToRead;
                    this.memoryBuffer.Add(temp);
                }
            }
        }

        private int readInternal(byte[] buffer, int offset, int count)
        {
            if (this.currentData == null || this.currentDataPosition >= this.currentData.Length)
            {
                try
                {
                    //blocks until data is received or throws InvalidOperationException if collection is completed.
                    this.currentData = this.memoryBuffer.Take();
                    this.currentDataPosition = 0;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            }

            int bytesToRead = buffer.Length - offset;
            if (bytesToRead > count)
            {
                bytesToRead = count;
            }

            if (this.currentDataPosition + bytesToRead > this.currentData.Length)
            {
                bytesToRead = this.currentData.Length - this.currentDataPosition;
            }

            Array.Copy(this.currentData, this.currentDataPosition, buffer, offset, bytesToRead);
            this.currentDataPosition += bytesToRead;
            return bytesToRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Seeking not supported");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Writing not supported");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Writing not supported");
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

        private void throttle(long currentTimeSlotInternal)
        {
            if (this.maximumBytesPerSecondRead > 0)
            {
                long minHistoryTimeSlot = currentTimeSlotInternal - ThrottledBufferedStream.historyForUploadInSeconds * ThrottledBufferedStream.timeSlotMultiplierForSeconds;

                var historyBytes = this.bytesPerTimeSlot.Where(kvp => kvp.Key > minHistoryTimeSlot).ToArray();

                if (historyBytes.Length > 1)
                {
                    long byteCountRead = historyBytes.Sum(kvp => kvp.Value);

                    // Calculate the current bytes in the defined history time span for upload speed.
                    long targetBytesInHistory = this.maximumBytesPerSecondRead * ThrottledBufferedStream.historyForUploadInSeconds;

                    // If the bytes are more then the maximum bytes, try to throttle.
                    if (byteCountRead > targetBytesInHistory)
                    {
                        // Calculate the time to sleep.
                        long bytesTooMuch = byteCountRead - targetBytesInHistory;
                        float sleep = (float)bytesTooMuch / this.maximumBytesPerSecondRead;
                        int toSleep = (int)(sleep * 1000);

                        if (toSleep > 1)
                        {
                            //ensure max sleep time 2 seconds, to continuously throttle down
                            //on small numbers and ensure further gui display.
                            //otherwise sleep times of several minutes would occur.
                            if (toSleep > 2000)
                            {
                                toSleep = 2000;
                            }

                            Thread.Sleep(toSleep);
                        }
                    }
                }
            }
        }
    }
}