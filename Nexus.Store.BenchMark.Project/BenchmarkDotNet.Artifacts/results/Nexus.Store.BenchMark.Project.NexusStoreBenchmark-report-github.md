```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7623/25H2/2025Update/HudsonValley2)
AMD Ryzen 7 4800H with Radeon Graphics 2.90GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3


```
| Method                      | TotalRecords | Mean        | Error     | StdDev      | Gen0      | Gen1      | Gen2     | Allocated  |
|---------------------------- |------------- |------------:|----------:|------------:|----------:|----------:|---------:|-----------:|
| **SetBatchAsync_Performance**   | **1000**         |  **2,251.5 μs** |  **44.64 μs** |    **90.18 μs** |  **203.1250** |   **74.2188** |  **23.4375** |  **1119598 B** |
| GetAsync_Random_Performance | 1000         |    155.5 μs |   3.07 μs |     6.80 μs |    0.2441 |         - |        - |      752 B |
| **SetBatchAsync_Performance**   | **10000**        | **26,461.8 μs** | **528.50 μs** | **1,171.12 μs** | **2593.7500** | **1031.2500** | **406.2500** | **11468552 B** |
| GetAsync_Random_Performance | 10000        |    171.3 μs |   4.09 μs |    12.06 μs |    0.2441 |         - |        - |      752 B |
