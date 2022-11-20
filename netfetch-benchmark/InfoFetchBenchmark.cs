using BenchmarkDotNet.Attributes;
using netfetch.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netfetch_benchmark
{
    public class InfoFetchBenchmark
    {
        [Benchmark]
        public void TestConstruction()
        {
            var info = new InfoFetch();
        }
    }
}
