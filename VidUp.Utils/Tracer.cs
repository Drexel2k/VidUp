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
            if (Settings.SettingsInstance.UserSettings.Trace)
            {
                Trace.WriteLine($"{DateTime.Now.ToString("dd.MM.yy hh:mm:ss.fff")}: {text}");
            }
        }
    }
}