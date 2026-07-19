using OctoMap.Runtime;

namespace OctoMap.Generation
{
    /// <summary>
    /// Provides compiled map dependencies required by generated mappers.
    /// </summary>
    public interface ICompiledMapDependencyProvider
    {
        /// <summary>
        /// Gets or creates a compiled child map for the specified source and destination types.
        /// </summary>
        /// <param name="sourceType">The child source type.</param>
        /// <param name="destinationType">The child destination type.</param>
        /// <returns>The compiled child map.</returns>
        CompiledMap GetOrAdd(Type sourceType, Type destinationType);
    }
}
