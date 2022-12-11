namespace sysfetch;

public class SysInfo
{
    public string? OSName { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public string? Kernel { get; set; } = string.Empty;
    
    public string Uptime { get; set; } = string.Empty;
    
    public IEnumerable<string>? CPU { get; set; } = null;
    
    public IEnumerable<string>? GPUs { get; set; } = null;

    public IEnumerable<string>? Resolution { get; set; } = null;
    
    public string Memory { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;
}