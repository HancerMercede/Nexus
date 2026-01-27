using Microsoft.Extensions.DependencyInjection;
using Nexus.Store.Contract;

namespace Nexus.Store;

/// <summary>
/// Extension methods for setting up Nexus.Store in an IServiceCollection.
/// Provides a seamless integration with .NET Dependency Injection.
/// </summary>
public static class NexusServiceExtensions
{
    /// <summary>
    /// Adds and configures the NexusEngine using a configuration delegate.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configure">An Action to configure the NexusOptions.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddNexusStore(this IServiceCollection services, Action<NexusOptions> configure)
    {
        var options = new NexusOptions();
        configure(options);
        
        return services.AddNexusStore(options);
    }

    /// <summary>
    /// Adds and configures the NexusEngine using a pre-constructed options instance.
    /// This overload supports init-only properties and direct object instantiation.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="options">The pre-configured NexusOptions instance.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddNexusStore(this IServiceCollection services, NexusOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));
        

        // Validate the options
        options.Validate();
        
        // Register the engine as a Singleton bound to its interface (INexusEngine).
        // This maintains the server lifecycle throughout the application's lifetime.
        var engine = new NexusEngine(options);
        services.AddSingleton<INexusEngine>(engine);
        
        return services;
    }
}