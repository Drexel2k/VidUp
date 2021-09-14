using System;
using System.Diagnostics;
using System.IO;

namespace Drexel.VidUp.Utils
{
    public static class Tracer
    {
        static Tracer()
        {
            string[] files = Directory.GetFiles(Settings.Instance.StorageFolder, "trace_*.txt");
            if (files.Length > 4)
            {
                Array.Reverse(files);
                for (int i = 4; i < files.Length; i++)
                {
                    File.Delete(files[i]);
                }
            }

            FileStream traceFileStream = File.Open(Path.Combine(Settings.Instance.StorageFolder, $"trace_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff")}.txt"), FileMode.Create);
            Trace.Listeners.Add(new TextWriterTraceListener(traceFileStream));
            Trace.AutoFlush = true;
        }

        public static void Write(string text)
        {
            Tracer.Write(text, TraceLevel.Normal);
        }

        public static void Write(string text, TraceLevel traceLevel)
        {
            if (Settings.Instance.UserSettings.Trace)
            {
                if (Settings.Instance.UserSettings.TraceLevel == TraceLevel.Normal && traceLevel == TraceLevel.Detailed)
                {
                    return;
                }

                Trace.WriteLine($"{DateTime.Now.ToString("dd.MM.yy HH:mm:ss.fff")}: {text}");
            }
        }

        public static void Close()
        {
            Trace.Close();
        }
    }
}