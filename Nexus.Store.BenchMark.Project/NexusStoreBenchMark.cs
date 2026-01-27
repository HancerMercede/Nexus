using BenchmarkDotNet.Attributes;
using Nexus.Store.BenchMark.Project.Entities;

namespace Nexus.Store.BenchMark.Project;

[MemoryDiagnoser] 
[JsonExporterAttribute.Brief] 
public class NexusStoreBenchmark
{
    private NexusEngine _nexus;
    private Dictionary<string, DevicePayload> _dataBatch;

    [Params(1000, 10000)] 
    public int TotalRecords;

    [GlobalSetup]
    public void Setup()
    {
        var options = new NexusOptions
        {
            Name = "BenchmarkedEngine",
            Port = 7006,
            MemoryLimit = "2g",
            StoragePath = Path.Combine(AppContext.BaseDirectory, "bench-data-pro")
        };
        _nexus = new NexusEngine(options);

        _dataBatch = new Dictionary<string, DevicePayload>();
        for (int i = 0; i < TotalRecords; i++)
        {
            _dataBatch.Add($"dev_id_{i}", new DevicePayload(i, "Sensor-X", i * 1.5, DateTime.UtcNow));
        }
    }

    [Benchmark]
    public async Task SetBatchAsync_Performance()
    {
        await _nexus.SetBatchAsync(_dataBatch);
    }

    [Benchmark]
    public async Task GetAsync_Random_Performance()
    {
        await _nexus.GetAsync<DevicePayload>($"dev_id_{TotalRecords / 2}");
    }

    [GlobalCleanup]
    public void Cleanup() => _nexus.Dispose();
}