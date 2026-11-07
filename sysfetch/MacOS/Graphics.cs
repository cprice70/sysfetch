using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace sysfetch.MacOS;

/// <summary>
/// GPU and display probes via IOKit and CoreGraphics (replaces system_profiler).
/// </summary>
[SupportedOSPlatform("macos")]
internal static unsafe partial class Graphics
{
    private const string IOKit = "/System/Library/Frameworks/IOKit.framework/IOKit";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    private const uint KCFStringEncodingUtf8 = 0x08000100;
    private const int KCFNumberSInt64Type = 4;

    public static List<string> GetGpus()
    {
        var gpus = new List<string>();
        var matching = IOServiceMatching("IOAccelerator");
        if (matching == 0) return gpus;

        // IOServiceGetMatchingServices consumes the matching dictionary.
        if (IOServiceGetMatchingServices(0, matching, out var iterator) != 0) return gpus;

        try
        {
            uint service;
            while ((service = IOIteratorNext(iterator)) != 0)
            {
                try
                {
                    var name = GetStringProperty(service, "model");
                    if (string.IsNullOrEmpty(name) && IORegistryEntryGetParentEntry(service, "IOService", out var parent) == 0)
                    {
                        // Intel/AMD GPUs keep the model on the parent PCI device.
                        name = GetStringProperty(parent, "model");
                        IOObjectRelease(parent);
                    }

                    if (string.IsNullOrEmpty(name) || gpus.Any(g => g.StartsWith(name, StringComparison.Ordinal))) continue;

                    var cores = GetInt64Property(service, "gpu-core-count");
                    gpus.Add(cores > 0 ? $"{name} ({cores} cores)" : name);
                }
                finally
                {
                    IOObjectRelease(service);
                }
            }
        }
        finally
        {
            IOObjectRelease(iterator);
        }

        return gpus;
    }

    public static List<string> GetResolutions()
    {
        var results = new List<string>();
        const uint maxDisplays = 16;
        var displays = stackalloc uint[(int)maxDisplays];
        if (CGGetActiveDisplayList(maxDisplays, displays, out var count) != 0) return results;

        for (var i = 0; i < count; i++)
        {
            var mode = CGDisplayCopyDisplayMode(displays[i]);
            if (mode == 0) continue;
            try
            {
                var width = CGDisplayModeGetPixelWidth(mode);
                var height = CGDisplayModeGetPixelHeight(mode);
                var hz = Math.Round(CGDisplayModeGetRefreshRate(mode));
                results.Add(hz > 0 ? $"{width}x{height} @ {hz}Hz" : $"{width}x{height}");
            }
            finally
            {
                CGDisplayModeRelease(mode);
            }
        }

        return results;
    }

    private static string? GetStringProperty(uint entry, string key)
    {
        var value = CopyProperty(entry, key);
        if (value == 0) return null;
        try
        {
            var type = CFGetTypeID(value);
            if (type == CFStringGetTypeID())
            {
                var buffer = stackalloc byte[256];
                return CFStringGetCString(value, buffer, 256, KCFStringEncodingUtf8)
                    ? Marshal.PtrToStringUTF8((nint)buffer)
                    : null;
            }

            if (type == CFDataGetTypeID())
            {
                var length = (int)CFDataGetLength(value);
                var bytes = new ReadOnlySpan<byte>((void*)CFDataGetBytePtr(value), length);
                var end = bytes.IndexOf((byte)0);
                return Encoding.UTF8.GetString(end < 0 ? bytes : bytes[..end]);
            }

            return null;
        }
        finally
        {
            CFRelease(value);
        }
    }

    private static long GetInt64Property(uint entry, string key)
    {
        var value = CopyProperty(entry, key);
        if (value == 0) return 0;
        try
        {
            long result = 0;
            return CFGetTypeID(value) == CFNumberGetTypeID() && CFNumberGetValue(value, KCFNumberSInt64Type, &result)
                ? result
                : 0;
        }
        finally
        {
            CFRelease(value);
        }
    }

    private static nint CopyProperty(uint entry, string key)
    {
        var cfKey = CFStringCreateWithCString(0, key, KCFStringEncodingUtf8);
        if (cfKey == 0) return 0;
        try
        {
            return IORegistryEntryCreateCFProperty(entry, cfKey, 0, 0);
        }
        finally
        {
            CFRelease(cfKey);
        }
    }

    // IOKit
    [LibraryImport(IOKit, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint IOServiceMatching(string name);

    [LibraryImport(IOKit)]
    private static partial int IOServiceGetMatchingServices(uint mainPort, nint matching, out uint iterator);

    [LibraryImport(IOKit)]
    private static partial uint IOIteratorNext(uint iterator);

    [LibraryImport(IOKit)]
    private static partial int IOObjectRelease(uint obj);

    [LibraryImport(IOKit, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int IORegistryEntryGetParentEntry(uint entry, string plane, out uint parent);

    [LibraryImport(IOKit)]
    private static partial nint IORegistryEntryCreateCFProperty(uint entry, nint key, nint allocator, uint options);

    // CoreFoundation
    [LibraryImport(CoreFoundation, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint CFStringCreateWithCString(nint allocator, string str, uint encoding);

    [LibraryImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool CFStringGetCString(nint str, byte* buffer, nint size, uint encoding);

    [LibraryImport(CoreFoundation)]
    private static partial nuint CFGetTypeID(nint cf);

    [LibraryImport(CoreFoundation)]
    private static partial nuint CFStringGetTypeID();

    [LibraryImport(CoreFoundation)]
    private static partial nuint CFDataGetTypeID();

    [LibraryImport(CoreFoundation)]
    private static partial nuint CFNumberGetTypeID();

    [LibraryImport(CoreFoundation)]
    private static partial nint CFDataGetLength(nint data);

    [LibraryImport(CoreFoundation)]
    private static partial nint CFDataGetBytePtr(nint data);

    [LibraryImport(CoreFoundation)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool CFNumberGetValue(nint number, int type, void* value);

    [LibraryImport(CoreFoundation)]
    private static partial void CFRelease(nint cf);

    // CoreGraphics
    [LibraryImport(CoreGraphics)]
    private static partial int CGGetActiveDisplayList(uint maxDisplays, uint* displays, out uint count);

    [LibraryImport(CoreGraphics)]
    private static partial nint CGDisplayCopyDisplayMode(uint display);

    [LibraryImport(CoreGraphics)]
    private static partial nuint CGDisplayModeGetPixelWidth(nint mode);

    [LibraryImport(CoreGraphics)]
    private static partial nuint CGDisplayModeGetPixelHeight(nint mode);

    [LibraryImport(CoreGraphics)]
    private static partial double CGDisplayModeGetRefreshRate(nint mode);

    [LibraryImport(CoreGraphics)]
    private static partial void CGDisplayModeRelease(nint mode);
}
