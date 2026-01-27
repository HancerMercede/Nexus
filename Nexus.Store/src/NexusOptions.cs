namespace Nexus.Store;

/// <summary>
/// Configuration settings for the Nexus Engine instance.
/// Using a record ensures immutability and thread-safety during initialization.
/// </summary>
public class NexusOptions
{
    /// <summary>
    /// Unique name for this engine instance. Used for telemetry and monitoring.
    /// </summary>
    public string Name { get; set; } = "nexus-default";

    /// <summary>
    /// IP address the Garnet server will bind to. Default is localhost.
    /// </summary>
    public string Address { get; set; } = "127.0.0.1";

    /// <summary>
    /// TCP port for the server to listen on. Default is 6379 (Standard Redis port).
    /// </summary>
    public int Port { get; set; } = 6379;

    /// <summary>
    /// Maximum memory allocated for the log (e.g., "512m", "1g"). Must be a power of 2.
    /// </summary>
    public string MemoryLimit { get; set; } = "512m";

    /// <summary>
    /// Size of the index portion of the hash table (e.g., "64m").
    /// </summary>
    public string IndexSize { get; set; } = "64m";

    /// <summary>
    /// Root directory path for data persistence and tiered storage.
    /// </summary>
    public string? StoragePath { get; set; }
    
    /// <summary>
    /// Validates the configuration parameters to ensure the engine starts correctly.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when a required setting is invalid.</exception>
    public void Validate()
    {
        if (Port <= 0) 
            throw new ArgumentException("Port must be greater than 0.");
            
        if (string.IsNullOrEmpty(Name)) 
            throw new ArgumentException("Engine name is mandatory.");
            
        if (string.IsNullOrEmpty(Address))
            throw new ArgumentException("Binding address cannot be empty.");
    }
}