using OctoMap.Generation;

namespace OctoMap.Runtime
{
    /// <summary>
    /// Provides cached access to compiled maps.
    /// </summary>
    public interface ICompiledMapRegistry
    {
        /// <summary>
        /// Gets or creates a compiled map for the specified source and destination types.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>The compiled map.</returns>
        CompiledMap GetOrAdd(Type sourceType, Type destinationType);

        /// <summary>
        /// Gets or creates a compiled map for the specified source set and destination type.
        /// </summary>
        /// <param name="sourceTypes">The source types.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>The compiled map.</returns>
        CompiledMap GetOrAdd(IReadOnlyList<Type> sourceTypes, Type destinationType);

        /// <summary>
        /// Compiles all explicitly configured maps that are not already compiled.
        /// </summary>
        void CompileConfiguredMaps();
    }
}
