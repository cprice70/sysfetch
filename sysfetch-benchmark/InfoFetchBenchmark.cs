using BenchmarkDotNet.Attributes;
using sysfetch;

namespace sysfetch_benchmark
{
    [MemoryDiagnoser]
    public class InfoFetchBenchmark
    {
        private static IInfoFetch Create() =>
            OperatingSystem.IsWindows() ? new sysfetch.Windows.InfoFetch()
            : OperatingSystem.IsMacOS() ? new sysfetch.MacOS.InfoFetch()
            : new sysfetch.Linux.InfoFetch();

        [Benchmark]
        public static void TestConstruction()
        {
            var info = Create();
        }

        [Benchmark]
        public static void TestGpu()
        {
            var info = Create();

            if (info.SysInfo.GPUs == null) return;
            foreach (var gpu in info.SysInfo.GPUs)
            {
                var test = gpu;
            }
        }
    }
}
