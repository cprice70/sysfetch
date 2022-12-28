using System.Diagnostics;

namespace sysfetch.Helpers
{
    public class CommandHelpers
    {
        public static string OutputFromCommand(string command, string arguments = "")
        {
            try
            {
                // Define the process information
                var pInfo = new ProcessStartInfo(command, arguments)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                // Create and run the process
                Process oProc = new();
                oProc.StartInfo = pInfo;
                oProc.Start();

                var sResult = oProc.StandardOutput.ReadToEnd();
                oProc.WaitForExit();
                return sResult;
            }
            catch (Exception ex)
            {
                Console.Write($"Exception: Running command {ex}");
                return string.Empty;
            }
        }

        public static bool IsCommandAvailable(string command)
        {
            return !string.IsNullOrEmpty(CommandHelpers.OutputFromCommand("which", command));
        }
    }
}
