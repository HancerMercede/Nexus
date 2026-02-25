using System.Buffers;
using Garnet;
using StackExchange.Redis;
using MessagePack;
using Microsoft.Extensions.Logging;
using Nexus.Store.Contract;

namespace Nexus.Store;

/// <summary>
/// The core engine of Nexus.Store that encapsulates Garnet and MessagePack.
/// </summary>
public class NexusEngine : INexusEngine
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
            "--aof",
            "--recover",
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
    /// Sets a value in the store associated with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="key">The unique identifier for the stored item.</param>
    /// <param name="value">The object to serialize and store.</param>
    /// <param name="expiry">Optional expiration time. If null, the item persists indefinitely (or until manual eviction).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        // Serialize the object to a high-performance binary format using MessagePack
        byte[] serializedData = MessagePackSerializer.Serialize(value);

        // If an expiry is provided, we use the TTL (Time To Live) feature of the underlying engine
        if (expiry.HasValue)
        {
            await _db.StringSetAsync(key, serializedData, expiry.Value);
        }
        else
        {
            // Stores the data permanently in the primary or tiered storage
            await _db.StringSetAsync(key, serializedData);
        }
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
        var writer = new ArrayBufferWriter<byte>(1024 * 4);
        
        foreach (var item in items)
        {
            _monitor.RecordOp();
            writer.Clear();
            
            MessagePackSerializer.Serialize(writer, item.Value);
            
            byte[] data = writer.WrittenSpan.ToArray();
            
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
        await RemoveTagAssociationsAsync(key);
        return await _db.KeyDeleteAsync(key);
    }

    /// <summary>
    /// Saves an object with associated tags for cache invalidation.
    /// </summary>
    public async Task SetAsync<T>(string key, T value, IEnumerable<string> tags)
    {
        CheckDisposed();
        _monitor.RecordOp();
        
        byte[] data = MessagePackSerializer.Serialize(value);
        await _db.StringSetAsync(key, data);
        await RegisterTagsAsync(key, tags);
    }

    /// <summary>
    /// Saves an object with associated tags and optional expiration.
    /// </summary>
    public async Task SetAsync<T>(string key, T value, IEnumerable<string> tags, TimeSpan? expiry = null)
    {
        CheckDisposed();
        _monitor.RecordOp();
        
        byte[] data = MessagePackSerializer.Serialize(value);
        
        if (expiry.HasValue)
        {
            await _db.StringSetAsync(key, data, expiry.Value);
        }
        else
        {
            await _db.StringSetAsync(key, data);
        }
        
        await RegisterTagsAsync(key, tags);
    }

    /// <summary>
    /// Invalidates all cache entries associated with the specified tag.
    /// </summary>
    public async Task<long> InvalidateByTagAsync(string tag)
    {
        CheckDisposed();
        string tagKey = GetTagKey(tag);
        
        var keys = await _db.SetMembersAsync(tagKey);
        if (keys.Length == 0) return 0;
        
        var keysToDelete = keys.Select(k => (RedisKey)k.ToString()).ToArray();
        await _db.KeyDeleteAsync(keysToDelete);
        await _db.KeyDeleteAsync(tagKey);
        
        return keys.Length;
    }

    /// <summary>
    /// Invalidates all cache entries associated with any of the specified tags.
    /// </summary>
    public async Task<long> InvalidateByTagsAsync(IEnumerable<string> tags)
    {
        CheckDisposed();
        var tagList = tags.ToList();
        long totalInvalidated = 0;
        
        var deletedKeys = new HashSet<RedisValue>();
        
        foreach (var tag in tagList)
        {
            string tagKey = GetTagKey(tag);
            var keys = await _db.SetMembersAsync(tagKey);
            
            foreach (var key in keys)
            {
                if (deletedKeys.Add(key))
                {
                    await _db.KeyDeleteAsync(key.ToString());
                    totalInvalidated++;
                }
            }
            
            await _db.KeyDeleteAsync(tagKey);
        }
        
        return totalInvalidated;
    }

    /// <summary>
    /// Gets all keys associated with a specific tag.
    /// </summary>
    public async Task<IEnumerable<string>> GetKeysByTagAsync(string tag)
    {
        CheckDisposed();
        string tagKey = GetTagKey(tag);
        var keys = await _db.SetMembersAsync(tagKey);
        return keys.Select(k => k.ToString());
    }

    private string GetTagKey(string tag) => $"__tag:{tag}__";

    private async Task RegisterTagsAsync(string key, IEnumerable<string> tags)
    {
        foreach (var tag in tags)
        {
            string tagKey = GetTagKey(tag);
            await _db.SetAddAsync(tagKey, key);
        }
    }

    private async Task RemoveTagAssociationsAsync(string key)
    {
        var tagKeys = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints()[0])
            .Keys(pattern: "__tag:*__")
            .Select(k => k.ToString());
        
        var tasks = new List<Task>();
        foreach (var tagKey in tagKeys)
        {
            tasks.Add(_db.SetRemoveAsync(tagKey, key));
        }
        await Task.WhenAll(tasks);
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