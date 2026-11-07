namespace sysfetch;

public class SysInfo
{
    public string? OSName { get; set; } = string.Empty;

    /// <summary>Lowercase OS identifier used to pick a logo, e.g. "macos", "windows", "ubuntu".</summary>
    public string OSId { get; set; } = string.Empty;

    /// <summary>Related distro identifiers (os-release ID_LIKE), used as logo fallbacks.</summary>
    public IReadOnlyList<string> OSIdLike { get; set; } = [];

    public string Host { get; set; } = string.Empty;

    public string? Kernel { get; set; } = string.Empty;

    public string Uptime { get; set; } = string.Empty;

    public IEnumerable<string?>? CPU { get; set; } = null;

    public IEnumerable<string>? GPUs { get; set; } = null;

    public IEnumerable<string>? Resolution { get; set; } = null;

    public string Memory { get; set; } = string.Empty;
}
