using System.Diagnostics.Metrics;

namespace Nexus.Store;

/// <summary>
/// Provides high-performance telemetry and instrumentation for the Nexus Engine.
/// Uses System.Diagnostics.Metrics for OpenTelemetry compatibility.
/// </summary>
public class NexusMonitor
{
    private readonly Meter _meter;
    private readonly Counter<long> _opsCounter;
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Histogram<double> _latencyHistogram;

    /// <summary>
    /// Initializes a new instance of the monitoring system for a specific engine.
    /// </summary>
    /// <param name="name">The unique name of the engine instance to monitor.</param>
    public NexusMonitor(string name)
    {
        // Meter name follows the standard dot-notation for observability
        _meter = new Meter($"Nexus.Store.{name}");
        
        // Counter for tracking throughput (Operations Per Second)
        _opsCounter = _meter.CreateCounter<long>(
            name: "nexus_ops_total", 
            unit: "ops", 
            description: "Total number of operations processed by the engine");
        
        // Counter for tracking successful cache lookups
        _cacheHits = _meter.CreateCounter<long>(
            name: "nexus_cache_hits_total",
            unit: "hits",
            description: "Total number of successful Garnet lookups (Hits)");

        // Counter for tracking failed cache lookups
        _cacheMisses = _meter.CreateCounter<long>(
            name: "nexus_cache_misses_total",
            unit: "misses",
            description: "Total number of lookups where data was not found (Misses)");

        // Histogram for tracking latency distribution.
        // This allows calculating percentiles (P95, P99) to identify slow requests.
        _latencyHistogram = _meter.CreateHistogram<double>(
            name: "nexus_op_duration_ms",
            unit: "ms",
            description: "Duration of operations in milliseconds");
    }
    /// <summary>
    /// Records a single operation in the metrics counter.
    /// This is a lock-free operation designed for high-concurrency environments.
    /// </summary>
    public void RecordOp() => _opsCounter.Add(1);
    /// <summary>
    /// Records a cache hit when data exists in Garnet.
    /// </summary>
    public void RecordHit() => _cacheHits.Add(1);

    /// <summary>
    /// Records a cache miss when data is absent and must be fetched from the source.
    /// </summary>
    public void RecordMiss() => _cacheMisses.Add(1);

    /// <summary>
    /// Records the execution time of an operation.
    /// </summary>
    /// <param name="ms">The elapsed time in milliseconds.</param>
    public void RecordLatency(double ms) => _latencyHistogram.Record(ms);
}