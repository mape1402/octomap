using Microsoft.Extensions.DependencyInjection;
using OctoMap.Generation;

namespace OctoMap.Runtime
{
    /// <summary>
    /// Resolves compiled map dependencies without creating a constructor cycle between the registry and generation backend.
    /// </summary>
    internal sealed class CompiledMapDependencyProvider : ICompiledMapDependencyProvider
    {
        private readonly IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompiledMapDependencyProvider"/> class.
        /// </summary>
        /// <param name="services">The application service provider.</param>
        public CompiledMapDependencyProvider(IServiceProvider services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <inheritdoc/>
        public CompiledMap GetOrAdd(Type sourceType, Type destinationType)
            => _services.GetRequiredService<ICompiledMapRegistry>().GetOrAdd(sourceType, destinationType);
    }
}
