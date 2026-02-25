namespace Nexus.Store.Contract;

/// <summary>
/// Defines the core operations for Nexus.Store high-performance caching 
/// leveraging Garnet and MessagePack serialization.
/// </summary>
public interface INexusEngine : IDisposable
{
    /// <summary>
    /// Saves an object serialized with MessagePack indefinitely.
    /// </summary>
    /// <typeparam name="T">The type of the object to store.</typeparam>
    /// <param name="key">The unique identifier for the stored item.</param>
    /// <param name="value">The object to serialize and store.</param>
    Task SetAsync<T>(string key, T value);

    /// <summary>
    /// Sets a value in the store associated with the specified key with an expiration time.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="key">The unique identifier for the stored item.</param>
    /// <param name="value">The object to serialize and store.</param>
    /// <param name="expiry">Optional expiration time (TTL). If null, the item persists indefinitely.</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

    /// <summary>
    /// Retrieves an object from the store and deserializes it using MessagePack.
    /// </summary>
    /// <typeparam name="T">The expected type of the object.</typeparam>
    /// <param name="key">The unique identifier for the stored item.</param>
    /// <returns>The deserialized object, or default if the key does not exist.</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Performs an optimized bulk insertion using Redis Pipelining.
    /// </summary>
    /// <typeparam name="T">The type of the values in the batch.</typeparam>
    /// <param name="items">A dictionary containing the keys and values to store.</param>
    Task SetBatchAsync<T>(IDictionary<string, T> items);

    /// <summary>
    /// Removes a specific key and its associated data from the store.
    /// </summary>
    /// <param name="key">The unique identifier to remove.</param>
    /// <returns>True if the key was removed; otherwise, false.</returns>
    Task<bool> RemoveAsync(string key);

    /// <summary>
    /// Saves an object with associated tags for cache invalidation.
    /// </summary>
    /// <typeparam name="T">The type of the object to store.</typeparam>
    /// <param name="key">The unique identifier for the stored item.</param>
    /// <param name="value">The object to serialize and store.</param>
    /// <param name="tags">Tags associated with this key for invalidation tracking.</param>
    Task SetAsync<T>(string key, T value, IEnumerable<string> tags);

    /// <summary>
    /// Saves an object with associated tags and optional expiration.
    /// </summary>
    /// <typeparam name="T">The type of the object to store.</typeparam>
    /// <param name="key">The unique identifier for the stored item.</param>
    /// <param name="value">The object to serialize and store.</param>
    /// <param name="tags">Tags associated with this key for invalidation tracking.</param>
    /// <param name="expiry">Optional expiration time.</param>
    Task SetAsync<T>(string key, T value, IEnumerable<string> tags, TimeSpan? expiry = null);

    /// <summary>
    /// Invalidates all cache entries associated with the specified tag.
    /// </summary>
    /// <param name="tag">The tag to invalidate.</param>
    /// <returns>The number of keys invalidated.</returns>
    Task<long> InvalidateByTagAsync(string tag);

    /// <summary>
    /// Invalidates all cache entries associated with any of the specified tags.
    /// </summary>
    /// <param name="tags">The tags to invalidate.</param>
    /// <returns>The number of keys invalidated.</returns>
    Task<long> InvalidateByTagsAsync(IEnumerable<string> tags);

    /// <summary>
    /// Gets all keys associated with a specific tag.
    /// </summary>
    /// <param name="tag">The tag to query.</param>
    /// <returns>Collection of keys associated with the tag.</returns>
    Task<IEnumerable<string>> GetKeysByTagAsync(string tag);
}