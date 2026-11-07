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

            var dev = info.Dev;
            writer.WriteStartObject("dev");
            writer.WriteStartObject("dotnet");
            WriteNullable(writer, "sdk", dev.DotnetSdk);
            writer.WriteNumber("sdkCount", dev.DotnetSdkCount);
            writer.WriteEndObject();
            WriteNullable(writer, "node", dev.Node);
            writer.WriteStartObject("python");
            WriteNullable(writer, "version", dev.Python);
            WriteNullable(writer, "source", dev.PythonSource);
            writer.WriteEndObject();
            writer.WriteStartObject("android");
            WriteNullable(writer, "api", dev.AndroidApi);
            WriteNullable(writer, "buildTools", dev.AndroidBuildTools);
            writer.WriteEndObject();
            writer.WriteStartObject("git");
            WriteNullable(writer, "repo", dev.GitRepo);
            WriteNullable(writer, "branch", dev.GitBranch);
            writer.WriteEndObject();
            writer.WriteEndObject();
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
