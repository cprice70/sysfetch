using System.Diagnostics;

namespace sysfetch.Helpers
{
    public static class CommandHelpers
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Runs a command and returns its standard output, or an empty string if the
        /// command cannot be started or does not finish within the timeout.
        /// </summary>
        public static string OutputFromCommand(string command, string arguments = "", TimeSpan? timeout = null)
        {
            try
            {
                var pInfo = new ProcessStartInfo(command, arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(pInfo);
                if (process is null) return string.Empty;

                var output = process.StandardOutput.ReadToEndAsync();
                _ = process.StandardError.ReadToEndAsync();

                if (!process.WaitForExit(timeout ?? DefaultTimeout))
                {
                    try { process.Kill(entireProcessTree: true); } catch { /* already exited */ }
                    return string.Empty;
                }

                return output.Result;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Checks whether an executable exists on PATH without spawning a process.
        /// </summary>
        public static bool IsCommandAvailable(string command)
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path)) return false;

            var extensions = OperatingSystem.IsWindows()
                ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE").Split(Path.PathSeparator)
                : [string.Empty];

            foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var ext in extensions)
                {
                    if (File.Exists(Path.Combine(dir, command + ext))) return true;
                }
            }

            return false;
        }
    }
}
