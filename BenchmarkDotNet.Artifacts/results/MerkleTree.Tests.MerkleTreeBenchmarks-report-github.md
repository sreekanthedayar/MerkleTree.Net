```

BenchmarkDotNet v0.15.4, Windows 11 (10.0.26200.8875)
AMD Ryzen 5 7535HS with Radeon Graphics 3.30GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.103
  [Host]   : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | NumberOfLeaves | Mean            | Error           | StdDev        | Gen0      | Gen1     | Gen2     | Allocated  |
|----------------- |--------------- |----------------:|----------------:|--------------:|----------:|---------:|---------:|-----------:|
| **BuildTree**        | **100**            |    **105,576.8 ns** |    **39,201.84 ns** |   **2,148.79 ns** |   **13.9160** |   **1.0986** |        **-** |   **116595 B** |
| AuditProof       | 100            |        366.9 ns |       149.54 ns |       8.20 ns |    0.1230 |        - |        - |     1033 B |
| ConsistencyProof | 100            |        132.1 ns |        58.10 ns |       3.18 ns |    0.0715 |        - |        - |      600 B |
| **BuildTree**        | **1000**           |  **1,066,593.2 ns** |   **501,708.53 ns** |  **27,500.34 ns** |  **142.5781** |  **44.9219** |        **-** |  **1203024 B** |
| AuditProof       | 1000           |      1,390.9 ns |       580.94 ns |      31.84 ns |    0.1869 |        - |        - |     1574 B |
| ConsistencyProof | 1000           |        186.6 ns |       251.00 ns |      13.76 ns |    0.1032 |        - |        - |      864 B |
| **BuildTree**        | **10000**          | **18,489,570.3 ns** | **1,888,023.68 ns** | **103,488.95 ns** | **1500.0000** | **625.0000** | **156.2500** | **11569309 B** |
| AuditProof       | 10000          |     10,770.8 ns |     1,950.64 ns |     106.92 ns |    0.2441 |        - |        - |     2076 B |
| ConsistencyProof | 10000          |        266.1 ns |       176.43 ns |       9.67 ns |    0.1230 |        - |        - |     1032 B |
