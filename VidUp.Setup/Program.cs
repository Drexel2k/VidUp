using System.Diagnostics;
using System.Reflection;

namespace Drexel.VidUp.Setup
{
    class Program
    {
        static void Main(string[] args)
        {
            string config = string.Empty;
            string platform = string.Empty;
            string version = string.Empty;
            string sourcePath = string.Empty;

            try
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Building Setup wit InnoSetup.");
                Console.ForegroundColor = originalColor;

#if DEBUG
                config = "Debug";
#endif

#if RELEASE
                config = "Release";
#endif

#if X64
                platform = "x64";
#endif

                if (string.IsNullOrWhiteSpace(platform))
                {
                    throw new ApplicationException("Only x64 setup is supported.");
                }

                sourcePath = Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}..\\..\\..\\..\\VidUp.UI\\bin\\{config}\\x64\\net6.0-windows\\");

                version = AssemblyName.GetAssemblyName(Path.Combine(sourcePath, "VidUp.dll")).Version.ToString();
                version = version.Remove(version.Length - 2);

                string innoExe = Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}..\\..\\..\\InnoBin\\ISCC.exe");
                string innoIss = Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}..\\..\\..\\setup.iss");
                string param = $"{innoIss} /DCONFIG=\"{config}\" /DVERSION=\"{version}\" /DSOURCEPATH=\"{sourcePath}\"";
                ProcessStartInfo info = new ProcessStartInfo(innoExe, param);
                info.UseShellExecute = false;

                int exitCode;
                using (Process p = Process.Start(info))
                {
                    p.WaitForExit();
                    exitCode = p.ExitCode;
                }

                if (exitCode > 0)
                {
                    throw new ApplicationException("InnoSetup failed.");
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine();
                Console.WriteLine($"Config: {config}");
                Console.WriteLine($"Platform: {platform}");
                Console.WriteLine($"Source path: {sourcePath}");
                Console.WriteLine($"Version: {version}");
                Console.WriteLine("Building Setup done.");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine(e.Message);
                Console.WriteLine($"Config: {config}");
                Console.WriteLine($"Platform: {platform}");
                Console.WriteLine($"Source path: {sourcePath}");
                Console.WriteLine($"Version: {version}");
                Console.WriteLine("Building Setup failed.");
            }

            Console.ReadKey();
        }
    }
}
