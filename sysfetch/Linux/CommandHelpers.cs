using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sysfetch.Linux
{
    public class CommandHelpers
    {
        public static string OutputFromCommand(string command, string arguments)
        {
            try
            {
                // Define the process information
                ProcessStartInfo pInfo = new ProcessStartInfo(command, arguments);
                pInfo.RedirectStandardOutput = true;
                pInfo.UseShellExecute = false;
                pInfo.CreateNoWindow = false;

                // Create and run the process
                Process oProc = new();
                oProc.StartInfo = pInfo;
                oProc.Start();

                string sResult = oProc.StandardOutput.ReadToEnd();
                return sResult;
            }
            catch (Exception ex)
            {
                Console.Write($"Exception: Running command {ex}");
                return string.Empty;
            }
        }
    }
}
