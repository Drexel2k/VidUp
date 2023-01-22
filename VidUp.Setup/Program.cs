using System.Diagnostics;
using System.Reflection;

namespace Drexel.VidUp.Setup
{
    class Program
    {
        static void Main(string[] args)
        {
            bool obfuscate = false;

            string dotfuscatorExe = Path.GetFullPath($@"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\PreEmptiveSolutions\DotfuscatorCE\dotfuscator.exe");
            string config = string.Empty;
            string platform = string.Empty;
            string version = string.Empty;
            string dotfuscatorOut = string.Empty;
            string innoIn = string.Empty;

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

                innoIn = Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}..\\..\\..\\..\\VidUp.UI\\bin\\{config}\\x64\\net6.0-windows\\");

                if (obfuscate)
                {
                    if (!File.Exists(dotfuscatorExe))
                    {
                        throw new ApplicationException("Dotfuscator not found.");
                    }

                    dotfuscatorOut = Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}..\\..\\..\\..\\VidUp.UI\\bin\\dotfuscated\\{config}\\x64\\");
                    innoIn = Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}..\\..\\..\\..\\VidUp.UI\\bin\\dotfuscated\\{config}\\x64\\net6.0-windows\\");

                    if (Directory.Exists(innoIn))
                    {
                        Directory.Delete(innoIn, true);
                    }

                    string dotfuscatorConfig = Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}..\\..\\..\\Dotfuscator{config}.xml");
                    string dotfuscatorFullParamter = $"{dotfuscatorConfig} -p=outdir={dotfuscatorOut}";
                    ProcessStartInfo dotfuscatorStartInfo = new ProcessStartInfo(dotfuscatorExe, dotfuscatorFullParamter);
                    dotfuscatorStartInfo.UseShellExecute = false;

                    int dotfuscatorExitCode;
                    using (Process p = Process.Start(dotfuscatorStartInfo))
                    {
                        p.WaitForExit();
                        dotfuscatorExitCode = p.ExitCode;
                    }

                    if (dotfuscatorExitCode > 0)
                    {
                        throw new ApplicationException("Dotfuscator failed.");
                    }
                }

                version = AssemblyName.GetAssemblyName(Path.Combine(innoIn, "VidUp.dll")).Version.ToString();
                version = version.Remove(version.Length - 2);

                string innoExe = Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}..\\..\\..\\InnoBin\\ISCC.exe");
                string innoConfig = Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}..\\..\\..\\setup.iss");
                string innoFullParamter = $"{innoConfig} /DCONFIG=\"{config}\" /DVERSION=\"{version}\" /DSOURCEPATH=\"{innoIn}\"";
                ProcessStartInfo innoStartInfo = new ProcessStartInfo(innoExe, innoFullParamter);
                innoStartInfo.UseShellExecute = false;

                int innoExitCode;
                using (Process p = Process.Start(innoStartInfo))
                {
                    p.WaitForExit();
                    innoExitCode = p.ExitCode;
                }

                if (innoExitCode > 0)
                {
                    throw new ApplicationException("InnoSetup failed.");
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine();
                Console.WriteLine($"Obfuscate: {obfuscate}");
                Console.WriteLine($"Config: {config}");
                Console.WriteLine($"Platform: {platform}");
                Console.WriteLine($"Source path: {innoIn}");
                Console.WriteLine($"Version: {version}");
                Console.WriteLine("Building Setup done.");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine(e.Message);
                Console.WriteLine($"Obfuscate: {obfuscate}");
                Console.WriteLine($"Config: {config}");
                Console.WriteLine($"Platform: {platform}");
                Console.WriteLine($"Source path: {innoIn}");
                Console.WriteLine($"Version: {version}");
                Console.WriteLine("Building Setup failed.");
            }

            Console.ReadKey();
        }
    }
}
