using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Drexel.VidUp.Youtube.VideoUploadService
{
    public class ThrottledBufferedStream : Stream
    {
        private const int historyForUploadInSeconds = 3;
        private const long tickMultiplierForSeconds = 10000000;
        private const int keepHistoryForInSeconds = 30;
        private const int historyForStatsInSeconds = 20;

        private BufferBlock<byte[]> readBuffer;
        private BufferBlock<byte[]> memoryBuffer;
        private int readCapacity = 40 * 1024 * 1024;
        private const int maxBufferBlockCount = 6;

        private byte[] currentData;
        private int currentDataPosition;
        private int currentDataSize = 10 * 1024 * 1024;

        private Stream baseStream;
        private long position;

        private long maximumBytesPerSecondRead;
        private Dictionary<long, int> bytesPerTick = new Dictionary<long, int>();

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
                if (this.bytesPerTick.Count <= 0)
                {
                    return 0;
                }

                long historyTicks = currentTicks - ThrottledBufferedStream.historyForStatsInSeconds * ThrottledBufferedStream.tickMultiplierForSeconds;
                KeyValuePair<long, int>[] historyBytes;
                long minTick;
                lock (this.bytesPerTick)
                {
                    historyBytes = this.bytesPerTick.Where(kvp => kvp.Key > historyTicks).ToArray();
                    minTick = this.bytesPerTick.Min(kvp => kvp.Key);
                }

                int sum = historyBytes.Sum(historyByte => historyByte.Value);


                if (minTick > this.currentTicks - ThrottledBufferedStream.historyForStatsInSeconds * ThrottledBufferedStream.tickMultiplierForSeconds)
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

            DataflowBlockOptions readBufferOptions = new DataflowBlockOptions();
            readBufferOptions.BoundedCapacity = 1;
            this.readBuffer = new BufferBlock<byte[]>(readBufferOptions);

            DataflowBlockOptions memoryBufferOptions = new DataflowBlockOptions();
            memoryBufferOptions.BoundedCapacity = ThrottledBufferedStream.maxBufferBlockCount;
            this.memoryBuffer = new BufferBlock<byte[]>(memoryBufferOptions);

            Task.Run(this.fillReadBuffer);
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

            long currentTicks = this.currentTicks;

            this.throttle(currentTicks);

            //clean history
            KeyValuePair<long, int>[] outdatedEntries = this.bytesPerTick.Where(kvp => kvp.Key < currentTicks - ThrottledBufferedStream.keepHistoryForInSeconds * ThrottledBufferedStream.tickMultiplierForSeconds).ToArray();
            lock (this.bytesPerTick)
            {
                foreach (var outdatedEntry in outdatedEntries)
                {
                    this.bytesPerTick.Remove(outdatedEntry.Key);
                }
            }

            int bytesRead = this.readInternal(buffer, offset, count);
            this.position += bytesRead;

            lock (this.bytesPerTick)
            {
                if (this.bytesPerTick.ContainsKey(currentTicks))
                {
                    this.bytesPerTick[currentTicks] += bytesRead;
                }
                else
                {
                    this.bytesPerTick.Add(currentTicks, bytesRead);
                }
            }

            return bytesRead;
        }

        private void fillReadBuffer()
        {
            while (true)
            {
                byte[] buffer = new byte[this.readCapacity];
                int bytesRead = this.baseStream.Read(buffer, 0, this.readCapacity);
                if (bytesRead == 0)
                {
                    this.readBuffer.Complete();
                    return;
                }

                if (bytesRead < this.readCapacity)
                {
                    byte[] temp = new byte[bytesRead];
                    Array.Copy(buffer, 0, temp, 0, bytesRead);
                    buffer = temp;
                }

                Task<bool> task = this.readBuffer.SendAsync(buffer);

                task.Wait();
                if (!task.Result)
                {
                    throw new Exception("Could not process upload file.");
                }
            }
        }

        private void fillMemoryBuffer()
        {
            try
            {
                while (true)
                {
                    //blocks until data is received or throws InvalidOperationException if queue is empty and completed.
                    byte[] buffer = this.readBuffer.Receive();

                    int remainder;
                    int limit = Math.DivRem(buffer.Length, (this.currentDataSize), out remainder) - 1;

                    for (int i = 0; i <= limit; i++)
                    {
                        byte[] bufferSmall = new byte[this.currentDataSize];
                        Array.Copy(buffer, this.currentDataSize * i, bufferSmall, 0, this.currentDataSize);
                        Task<bool> task = memoryBuffer.SendAsync(bufferSmall);

                        task.Wait();
                        if (!task.Result)
                        {
                            throw new Exception("Could not process upload file.");
                        }
                    }

                    if (remainder > 0)
                    {
                        byte[] bufferSmall = new byte[remainder];
                        Array.Copy(buffer, this.currentDataSize * (limit + 1), bufferSmall, 0, remainder);
                        Task<bool> task = memoryBuffer.SendAsync(bufferSmall);

                        task.Wait();
                        if (!task.Result)
                        {
                            throw new Exception("Could not process upload file.");
                        }
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                this.memoryBuffer.Complete();
                return;
            }
        }

        private int readInternal(byte[] buffer, int offset, int count)
        {
            if (this.currentData == null || this.currentDataPosition == this.currentData.Length)
            {
                try
                {
                    //blocks until data is received or throws InvalidOperationException if queue is empty and completed.
                    this.currentData = this.memoryBuffer.Receive();
                    this.currentDataPosition = 0;
                }
                catch (InvalidOperationException e)
                {
                    return 0;
                }
            }

            int bytesToRead = buffer.Length - offset;
            if (bytesToRead > count)
            {
                bytesToRead = count;
            }

            int bytesLeftInCurrentData = this.currentData.Length - this.currentDataPosition;
            if (bytesToRead <= bytesLeftInCurrentData)
            {
                Array.Copy(this.currentData, this.currentDataPosition, buffer, offset, bytesToRead);
                this.currentDataPosition += bytesToRead;
                return bytesToRead;
            }
            else
            {
                Array.Copy(this.currentData, this.currentDataPosition, buffer, offset, bytesLeftInCurrentData);
                this.currentDataPosition += bytesLeftInCurrentData;
                return bytesLeftInCurrentData;
            }
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

        private void throttle(long currentTicks)
        {
            if (this.maximumBytesPerSecondRead > 0)
            {
                long historyTicks = currentTicks - ThrottledBufferedStream.historyForUploadInSeconds * ThrottledBufferedStream.tickMultiplierForSeconds;

                var historyBytes = this.bytesPerTick.Where(kvp => kvp.Key > historyTicks).ToArray();

                if (historyBytes.Length > 1)
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