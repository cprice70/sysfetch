namespace sysfetch.Linux.Helpers;

/// <summary>
/// Reads GPU, display and CPU details straight from sysfs/procfs. Root paths are
/// parameters so the parsing can be tested against fixture directories.
/// </summary>
public static class LinuxHardware
{
    public static readonly string[] DefaultPciIdsPaths =
    [
        "/usr/share/hwdata/pci.ids",
        "/usr/share/misc/pci.ids",
        "/usr/share/pci.ids",
        "/var/lib/pciutils/pci.ids"
    ];

    public static List<string> GetGpusFromSysfs(string pciRoot = "/sys/bus/pci/devices", IEnumerable<string>? pciIdsPaths = null)
    {
        var devices = new List<(string vendor, string device)>();
        if (!Directory.Exists(pciRoot)) return [];

        foreach (var dir in Directory.EnumerateDirectories(pciRoot).Order(StringComparer.Ordinal))
        {
            // PCI class 0x03xxxx = display controller (VGA, 3D, other).
            if (!ReadHex(Path.Combine(dir, "class")).StartsWith("03", StringComparison.Ordinal)) continue;

            var vendor = ReadHex(Path.Combine(dir, "vendor"));
            var device = ReadHex(Path.Combine(dir, "device"));
            if (vendor.Length > 0) devices.Add((vendor, device));
        }

        if (devices.Count == 0) return [];

        var idsPath = (pciIdsPaths ?? DefaultPciIdsPaths).FirstOrDefault(File.Exists);
        var names = idsPath is null ? new Dictionary<string, string>() : LookupPciIds(idsPath, devices);

        var gpus = new List<string>();
        foreach (var (vendor, device) in devices)
        {
            names.TryGetValue(vendor, out var vendorName);
            names.TryGetValue($"{vendor}:{device}", out var deviceName);
            var gpu = FormatGpu(vendor, vendorName, device, deviceName);
            if (!gpus.Contains(gpu)) gpus.Add(gpu);
        }

        return gpus;
    }

    /// <summary>
    /// Single pass over pci.ids collecting names for the requested vendors ("10de")
    /// and devices ("10de:1b80").
    /// </summary>
    public static Dictionary<string, string> LookupPciIds(string path, IReadOnlyCollection<(string vendor, string device)> wanted)
    {
        var result = new Dictionary<string, string>();
        var vendors = wanted.Select(w => w.vendor).ToHashSet();
        var pending = wanted.Select(w => $"{w.vendor}:{w.device}").ToHashSet();
        string? currentVendor = null;

        foreach (var line in File.ReadLines(path))
        {
            if (line.Length < 6 || line[0] == '#') continue;

            if (line[0] != '\t')
            {
                if (currentVendor is not null && pending.Count == 0) break;
                if (line.StartsWith("C ", StringComparison.Ordinal)) break; // device classes section follows vendors
                var id = line[..4].ToLowerInvariant();
                currentVendor = vendors.Contains(id) ? id : null;
                if (currentVendor is not null) result[id] = line[4..].Trim();
                continue;
            }

            // "\tdddd  Device name" (sub-devices use two tabs and are ignored).
            if (currentVendor is null || line.Length < 7 || line[1] == '\t') continue;
            var key = $"{currentVendor}:{line.Substring(1, 4).ToLowerInvariant()}";
            if (pending.Remove(key)) result[key] = line[5..].Trim();
        }

        return result;
    }

    public static string FormatGpu(string vendorId, string? vendorName, string deviceId, string? deviceName)
    {
        var vendor = vendorId switch
        {
            "10de" => "NVIDIA",
            "1002" or "1022" => "AMD",
            "8086" => "Intel",
            "15ad" => "VMware",
            "80ee" => "VirtualBox",
            "1234" => "QEMU",
            "1af4" => "Virtio",
            "1414" => "Microsoft",
            _ => CleanVendor(vendorName) ?? $"Vendor {vendorId}"
        };

        if (string.IsNullOrEmpty(deviceName)) return $"{vendor} Device {deviceId}";

        // "GP104 [GeForce GTX 1080]" -> "GeForce GTX 1080"
        var open = deviceName.LastIndexOf('[');
        var close = deviceName.LastIndexOf(']');
        var model = open >= 0 && close > open ? deviceName[(open + 1)..close].Trim() : deviceName.Trim();

        return model.StartsWith(vendor, StringComparison.OrdinalIgnoreCase) ? model : $"{vendor} {model}";
    }

    private static string? CleanVendor(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        foreach (var suffix in new[] { " Corporation", " Corp.", ", Inc.", " Inc.", " Co., Ltd.", " Ltd." })
            name = name.Replace(suffix, string.Empty);
        return name.Trim();
    }

    public static List<string> GetResolutionsFromDrm(string drmRoot = "/sys/class/drm")
    {
        var results = new List<string>();
        if (!Directory.Exists(drmRoot)) return results;

        // Connector entries look like "card0-HDMI-A-1".
        foreach (var dir in Directory.EnumerateDirectories(drmRoot).Order(StringComparer.Ordinal))
        {
            if (!Path.GetFileName(dir).Contains('-')) continue;
            if (ReadFirstLine(Path.Combine(dir, "status")) != "connected") continue;

            var mode = ReadFirstLine(Path.Combine(dir, "modes"));
            if (!string.IsNullOrEmpty(mode)) results.Add(mode);
        }

        return results;
    }

    /// <summary>
    /// CPU name from /proc/cpuinfo with fallbacks for ARM boards that omit "model name".
    /// </summary>
    public static string? ParseCpuName(string cpuInfo)
    {
        foreach (var key in new[] { "model name", "Hardware", "Processor", "cpu model", "cpu" })
        {
            foreach (var line in cpuInfo.Split('\n'))
            {
                var colon = line.IndexOf(':');
                if (colon < 0 || line[..colon].Trim() != key) continue;
                var value = line[(colon + 1)..].Trim();
                if (value.Length > 0) return value;
            }
        }

        return null;
    }

    private static string ReadHex(string path)
    {
        var value = ReadFirstLine(path);
        return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..].ToLowerInvariant() : value.ToLowerInvariant();
    }

    private static string ReadFirstLine(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadLines(path).FirstOrDefault()?.Trim() ?? string.Empty : string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
