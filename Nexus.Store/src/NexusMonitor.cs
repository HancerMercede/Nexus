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
    }

    /// <summary>
    /// Records a single operation in the metrics counter.
    /// This is a lock-free operation designed for high-concurrency environments.
    /// </summary>
    public void RecordOp() => _opsCounter.Add(1);
}