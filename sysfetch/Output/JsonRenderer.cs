using System.Text;
using System.Text.Json;

namespace sysfetch.Output;

/// <summary>
/// Writes SysInfo as JSON with Utf8JsonWriter (no reflection, so it is Native AOT safe).
/// </summary>
public static class JsonRenderer
{
    public static string Render(SysInfo info, string user, string hostName)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("user", user);
            writer.WriteString("hostname", hostName);
            WriteNullable(writer, "os", info.OSName);
            writer.WriteString("osId", info.OSId);
            WriteNullable(writer, "host", info.Host);
            WriteNullable(writer, "kernel", info.Kernel);
            WriteNullable(writer, "uptime", info.Uptime);
            WriteArray(writer, "cpus", info.CPU);
            WriteArray(writer, "gpus", info.GPUs);
            WriteArray(writer, "resolutions", info.Resolution);
            WriteNullable(writer, "memory", info.Memory);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }

    private static void WriteNullable(Utf8JsonWriter writer, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) writer.WriteNull(name);
        else writer.WriteString(name, value);
    }

    private static void WriteArray(Utf8JsonWriter writer, string name, IEnumerable<string?>? values)
    {
        writer.WriteStartArray(name);
        foreach (var value in values ?? [])
        {
            if (!string.IsNullOrWhiteSpace(value)) writer.WriteStringValue(value);
        }
        writer.WriteEndArray();
    }
}
