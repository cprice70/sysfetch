``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.25247.1000)
Intel Core i7-10750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 6.0.11 (6.0.1122.52304), X64 RyuJIT AVX2
  DefaultJob : .NET 6.0.11 (6.0.1122.52304), X64 RyuJIT AVX2


```
|           Method |    Mean |    Error |   StdDev |
|----------------- |--------:|---------:|---------:|
| TestConstruction | 1.169 s | 0.0050 s | 0.0044 s |
