using System;
using System.Diagnostics;
using System.IO;

namespace Drexel.VidUp.Utils
{
    public static class Tracer
    {
        static Tracer()
        {
            FileStream traceFileStream = File.Open(Path.Combine(Settings.SettingsInstance.StorageFolder, "trace.txt"), FileMode.Create);
            Trace.Listeners.Add(new TextWriterTraceListener(traceFileStream));
            Trace.AutoFlush = true;
        }

        public static void Write(string text)
        {
            Tracer.Write(text, TraceLevel.Normal);
        }

        public static void Write(string text, TraceLevel traceLevel)
        {
            if (Settings.SettingsInstance.UserSettings.Trace)
            {
                if (Settings.SettingsInstance.UserSettings.TraceLevel == TraceLevel.Normal && traceLevel == TraceLevel.Detailed)
                {
                    return;
                }

                Trace.WriteLine($"{DateTime.Now.ToString("dd.MM.yy hh:mm:ss.fff")}: {text}");
                Trace.WriteLine("", "");
            }
        }
    }
}