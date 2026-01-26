using Microsoft.Extensions.DependencyInjection;

namespace Nexus.Store;

/// <summary>
/// Extension methods for setting up Nexus.Store in an IServiceCollection.
/// Provides a seamless integration with .NET Dependency Injection.
/// </summary>
public static class NexusServiceExtensions
{
    /// <summary>
    /// Adds and configures the NexusEngine as a Singleton service.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configure">An Action to configure the NexusOptions.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddNexusStore(this IServiceCollection services, Action<NexusOptions> configure)
    {
        // 1. Initialize options with default values
        var options = new NexusOptions();
        
        // 2. Apply user-defined configuration
        configure(options);
        
        // 3. Register the engine as a Singleton to maintain the server lifecycle
        // throughout the application's lifetime.
        services.AddSingleton(new NexusEngine(options));
        
        return services;
    }
}