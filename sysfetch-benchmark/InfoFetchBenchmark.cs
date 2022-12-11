using BenchmarkDotNet.Attributes;
using sysfetch.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netfetch_benchmark
{
    [MemoryDiagnoser]
    public class InfoFetchBenchmark
    {
        [Benchmark]
        public void TestConstruction()
        {
            var info = new InfoFetch();
        }

        [Benchmark]
        public void TestGpu()
        {
            var info = new InfoFetch();

            foreach ( var gpu in info.SysInfo.GPUs)
            { 
                var test = gpu;
            }
        }
    }
}
