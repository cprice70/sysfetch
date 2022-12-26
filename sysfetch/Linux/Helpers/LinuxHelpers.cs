using System.Text;
using sysfetch.Extensions;

namespace sysfetch.Linux
{
    public class LinuxHelpers
    {
        public static string ParseDistroStrings(string[] fileInfo, string distroShorthand)
        {
            var distroBuilder = new StringBuilder();
            var distro = string.Empty;
            foreach (var info in fileInfo)
            {
                if (string.IsNullOrWhiteSpace(info)) continue;
                var lines = info.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                Dictionary<string, string> values =
                    lines.Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);
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
    }
}