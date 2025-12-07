using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;

namespace Umbraco.Community.CSPManager.Benchmarks;

/*
BenchmarkDotNet v0.15.2, Windows 10 (10.0.19045.6216/22H2/2022Update)
Intel Core i7-4770K CPU 3.50GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2


| Method    | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| ByteArray | 177.0 ns | 3.36 ns | 3.30 ns |  1.00 |    0.03 | 0.0267 |     112 B |        1.00 |
| ByteSpan  | 120.6 ns | 2.41 ns | 2.58 ns |  0.68 |    0.02 | 0.0172 |      72 B |        0.64 |
 */

[MemoryDiagnoser]
public class NonceGeneration
{
	[Benchmark(Baseline = true)]
	public string ByteArray()
	{
		using var rng = RandomNumberGenerator.Create();
		var nonceBytes = new byte[16];
		rng.GetBytes(nonceBytes);
		return Convert.ToBase64String(nonceBytes);
	}

	[Benchmark]
	public string ByteSpan()
	{
		Span<byte> nonceBytes = stackalloc byte[16];
		RandomNumberGenerator.Fill(nonceBytes);
		return Convert.ToBase64String(nonceBytes);
	}
}