using BenchmarkDotNet.Attributes;
using sysfetch.Windows;

namespace sysfetch_benchmark
{
    [MemoryDiagnoser]
    public class InfoFetchBenchmark
    {
        [Benchmark]
        public static void TestConstruction()
        {
            var info = new InfoFetch();
        }

        [Benchmark]
        public static void TestGpu()
        {
            var info = new InfoFetch();

            if (info.SysInfo.GPUs == null) return;
            foreach (var gpu in info.SysInfo.GPUs)
            {
                var test = gpu;
            }
        }
    }
}
