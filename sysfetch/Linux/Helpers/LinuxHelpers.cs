using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using sysfetch.Extensions;

namespace sysfetch.Linux.Helpers
{
    public partial class LinuxHelpers
    {
        public static string ParseDistroStrings(string[] fileInfo, string distroShorthand)
        {
            var distroBuilder = new StringBuilder();
            var distro = string.Empty;
            foreach (var info in fileInfo)
            {
                if (string.IsNullOrWhiteSpace(info)) continue;
                var lines = info.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var values =
                    lines.Select(x => x.Split('=', 2))
                        .Where(x => x.Length == 2)
                        .GroupBy(x => x[0].Trim())
                        .ToDictionary(g => g.Key, g => g.First()[1].Trim());
                switch (distroShorthand)
                {
                    case "on":
                        distroBuilder.Append(values.ContainsKey("NAME")
                            ? values["NAME"]
                            : values["DISTRIB_ID"]);
                        distroBuilder.Append(' ');
                        distroBuilder.Append(values.ContainsKey("VERSION_ID")
                            ? values["VERSION_ID"]
                            : values["DISTRIB_RELEASE"]);
                        return distroBuilder.ToString().RemoveNewlines().Replace("\"", string.Empty);
                    case "tiny":
                        distroBuilder.Append(values.ContainsKey("NAME") ? values["NAME"] : values["DISTRIB_ID"]);
                        if (!values.ContainsKey("NAME"))
                        {
                            distroBuilder.Append(values.ContainsKey("TAILS_PRODUCT_NAME")
                                ? values["TAILS_PRODUCT_NAME"]
                                : distro);
                        }
                        return distroBuilder.ToString().RemoveNewlines().Replace("\"", string.Empty);
                    case "off":
                        distroBuilder.Append(values.ContainsKey("PRETTY_NAME")
                            ? values["PRETTY_NAME"]
                            : values["DISTRIB_DESCRIPTION"]);
                        if (!values.ContainsKey("UBUNTU_CODENAME"))
                            return distroBuilder.ToString().RemoveNewlines().Replace("\"", string.Empty);
                        distroBuilder.Append(" ");
                        distroBuilder.Append(values["UBUNTU_CODENAME"]);
                        return distroBuilder.ToString().RemoveNewlines().Replace("\"", string.Empty);
                }
                break;
            }
            return distro;
        }

        /// <summary>
        /// Reads ID and ID_LIKE from os-release contents (lowercased, quotes removed).
        /// Defaults to "linux" when no ID is present.
        /// </summary>
        public static (string id, IReadOnlyList<string> like) ParseOsReleaseId(string osRelease)
        {
            string id = "linux";
            IReadOnlyList<string> like = [];

            foreach (var raw in osRelease.Split('\n'))
            {
                var parts = raw.Split('=', 2);
                if (parts.Length != 2) continue;
                var value = parts[1].Trim().Trim('"', '\'').ToLowerInvariant();
                switch (parts[0].Trim())
                {
                    case "ID" when value.Length > 0:
                        id = value;
                        break;
                    case "ID_LIKE":
                        like = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        break;
                }
            }

            return (id, like);
        }

        /// <summary>
        /// Computes used/total memory from /proc/meminfo contents, in MiB.
        /// Prefers MemAvailable (kernel 3.14+); falls back to the screenFetch formula:
        /// used = MemTotal + Shmem - MemFree - Buffers - Cached - SReclaimable.
        /// </summary>
        public static string ParseMemInfo(string memInfo)
        {
            long Read(string key)
            {
                foreach (var line in memInfo.Split('\n'))
                {
                    if (!line.StartsWith(key + ":", StringComparison.Ordinal)) continue;
                    var digits = new string(line.AsSpan(key.Length + 1).ToArray().Where(char.IsDigit).ToArray());
                    return long.TryParse(digits, out var v) ? v : 0;
                }
                return -1;
            }

            var memTotal = Read("MemTotal");
            if (memTotal <= 0) return string.Empty;

            var memAvailable = Read("MemAvailable");
            long memUsed = memAvailable >= 0
                ? memTotal - memAvailable
                : memTotal + Math.Max(0, Read("Shmem")) - Math.Max(0, Read("MemFree")) - Math.Max(0, Read("Buffers"))
                  - Math.Max(0, Read("Cached")) - Math.Max(0, Read("SReclaimable"));

            return $"{memUsed / 1024} MiB / {memTotal / 1024} MiB";
        }

        // Remove duplicate Intel Graphics outputs.
        // This fixes cases where the outputs are both
        // Intel but not entirely identical.
        //
        // Checking the first two array elements should
        // be safe since there won't be 2 intel outputs if
        // there's a dedicated GPU in play.
        public static IEnumerable<string> ParseGPUStrings(string[] gpus, string gpuType)
        {
            var collection = new List<string>();      
            try
            {
                if (gpus.Length > 1 && gpus[0].Contains("Intel") && gpus[1].Contains("Intel"))
                {
                    gpus = gpus.Skip(1).ToArray();
                }

                foreach (string gpu in gpus)
                {
                    var gpuTemp = gpu;
                    // GPU shorthand tests.
                    if (gpuType == "dedicated" && gpu.Contains("Intel") || gpuType == "integrated" && !gpu.Contains("Intel"))
                    {
                        continue;
                    }

                    var brand = string.Empty;
                    if (gpu.Contains("Advanced"))
                    {
                        brand = gpu.Contains("AMD") || gpu.Contains("ATI") ? "AMD ATI" : gpu.Contains("AMD") ? "AMD" : "ATi";
                        gpuTemp = gpu.Split("Advanced")[1];
                        const string pattern2 = @"\[(.*?)\]";
                        MatchCollection match = Regex.Matches(gpuTemp, pattern2);
                        gpuTemp = match[^2].Value;
                        gpuTemp = brand + " " + gpuTemp.Trim('[', ']' );
                    }
                    else if (gpu.Contains("Intel") || gpu.Contains("NVIDIA"))
                    {
                        MatchCollection matches = MyRegex().Matches(gpuTemp);
                        if (matches.Count < 1) continue;
                        
                        gpuTemp = matches[^1].Value.Trim('"');

                        if (gpu.Contains("NVIDIA"))
                        {
                            const string pattern2 = @"\[(.*?)\]";
                            Match match = Regex.Match(gpuTemp, pattern2);
                            gpuTemp = "NVIDIA " + match.Value.Trim('[', ']');
                        } 
                        else if (gpu.Contains("Intel"))
                        {
                            gpuTemp = "Intel " + gpuTemp;
                        }
                    }
                    else if (gpuTemp.Contains("MCST"))
                    {
                        gpuTemp = "MCST MGA2";
                    }
                    else if (gpuTemp.Contains("VirtualBox"))
                    {
                        gpuTemp = "VirtualBox Graphics Adapter";
                    }
                    else if (gpuType == "off")
                    {
                        gpuTemp = Regex.Replace(gpuTemp, "AMD|NVIDIA|Intel", "");
                    }
                    else
                    {
                        continue;
                    }
                    collection.Add(gpuTemp);
                   // yield return gpuTemp;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            return collection;
        }

        //Convert Input from device string to array of GPUs
        public static IEnumerable<string> ParseDeviceString(string input)
        {
            IList<string> gpus = new List<string>();
              // Use a regular expression to match lines containing the strings "Display", "3D", or "VGA"
            Regex regex = new Regex(@"Display|3D|VGA");

            // Use a dictionary to store the fields for each matching line
            Dictionary<string, string> fields = new Dictionary<string, string>();

            // Read the input line by line
            var lines = input.Split('\n');
            foreach (var line in lines)
            {
                // Check if the line matches the regular expression
                if (!regex.IsMatch(line)) continue;
                
                string pattern = "\"(.*?)\"";
                Regex quoated_regex = new Regex(pattern);
                MatchCollection matches = quoated_regex.Matches(line);
                var parts = matches.ToArray();
                
                string fieldValue;
                if (parts[^1].Value.Trim() == "" || Regex.IsMatch(parts[^1].Value.Trim(), @"^Device [[:xdigit:]]+$"))
                {
                    fieldValue = $"{parts[1]} {parts[2]} {parts[3]}";
                }
                else switch (parts.Length)
                {
                    case 5:
                    case 4:
                    case 3:
                        fieldValue = $"{parts[1]} {parts[^1]}";
                        break;
                    case 2:
                        fieldValue = $"{parts[^1]}";
                        break;
                    default:
                        continue;
                }
                
                fields[line] = fieldValue;
            }
            
            return fields.Values;
        }

        [GeneratedRegex("\"[^\"]*\"")]
        private static partial Regex MyRegex();
    }
}