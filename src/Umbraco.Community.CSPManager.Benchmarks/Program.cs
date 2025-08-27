
using BenchmarkDotNet.Running;
using Umbraco.Community.CSPManager.Benchmarks;

var summary = BenchmarkRunner.Run<NonceGeneration>();
Console.WriteLine(summary);