using BenchmarkDotNet.Attributes;

namespace OctoMap.Benchmarks
{
    [MemoryDiagnoser]
    public class MapperGenerationBenchmarks
    {
        [Benchmark]
        public string AssemblyName()
        {
            return typeof(OctoMapMarker).Assembly.GetName().Name!;
        }
    }
}
