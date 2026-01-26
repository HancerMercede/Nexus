
using Garnet;
using StackExchange.Redis;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace Nexus.Store;

/// <summary>
/// The core engine of Nexus.Store that encapsulates Garnet and MessagePack.
/// </summary>
public class NexusEngine : IDisposable
{
    private readonly GarnetServer _server;
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly NexusMonitor _monitor;
    private bool _disposed;

    public NexusEngine(NexusOptions options)
    {
        // 1. Validate options (Immutable since it's a record)
        options.Validate();
        
        _monitor = new NexusMonitor(options.Name);

        // Define storage paths
        string rootPath = Path.GetFullPath(options.StoragePath ?? Path.Combine(AppContext.BaseDirectory, "nexus-data"));
        string checkpointDir = Path.Combine(rootPath, "checkpoints");
        string logDir = Path.Combine(rootPath, "logs");
        
        // Ensure directories exist
        if (!Directory.Exists(checkpointDir)) Directory.CreateDirectory(checkpointDir);
        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        // 3. Garnet v2 configuration via Arguments (Most stable way in Beta)
        // This solves the issue where Address and Port are not direct properties
        var garnetArgs = new[]
        {
            "--bind", options.Address,
            "--port", options.Port.ToString(),
            "--memory", options.MemoryLimit,
            "--index", options.IndexSize,
            "--storage-tier",
            "--checkpointdir", checkpointDir,
            "--logdir", logDir
        };

        try
        {
            _server = new GarnetServer(garnetArgs);
            _server.Start();
        }
        catch (Exception ex)
        {
            throw new Exception($"Garnet engine initialization failed: {ex.Message}", ex);
        }

        // 4. Connect the internal Redis client with retries
        var redisConfig = new ConfigurationOptions
        {
            EndPoints = { { options.Address, options.Port } },
            AllowAdmin = true,
            AbortOnConnectFail = false,
            ConnectTimeout = 5000
        };

        _redis = ConnectionMultiplexer.Connect(redisConfig);
        _db = _redis.GetDatabase();
    }

    /// <summary>
    /// Saves an object serialized with MessagePack.
    /// </summary>
    public async Task SetAsync<T>(string key, T value)
    {
        CheckDisposed();
        _monitor.RecordOp();
        
        // Ultra-fast binary serialization
        byte[] data = MessagePackSerializer.Serialize(value);
        await _db.StringSetAsync(key, data);
    }

    /// <summary>
    /// Retrieves an object and deserializes it.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        CheckDisposed();
        _monitor.RecordOp();
        
        byte[]? data = await _db.StringGetAsync(key);
        if (data == null) return default;
        
        return MessagePackSerializer.Deserialize<T>(data);
    }

    /// <summary>
    /// Optimized bulk insertion via Pipelining.
    /// </summary>
    public async Task SetBatchAsync<T>(IDictionary<string, T> items)
    {
        CheckDisposed();
        var batch = _db.CreateBatch();
        
        foreach (var item in items)
        {
            _monitor.RecordOp();
            byte[] data = MessagePackSerializer.Serialize(item.Value);
            _ = batch.StringSetAsync(item.Key, data);
        }
        
        batch.Execute();
        await Task.CompletedTask; // SE.Redis batches are internal fire-and-forget
    }

    /// <summary>
    /// Removes a key from the cache.
    /// </summary>
    public async Task<bool> RemoveAsync(string key)
    {
        CheckDisposed();
        return await _db.KeyDeleteAsync(key);
    }

    private void CheckDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(NexusEngine));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _redis?.Dispose();
        _server?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}