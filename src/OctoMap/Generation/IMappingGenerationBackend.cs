using OctoMap.Planning;

namespace OctoMap.Generation
{
    /// <summary>
    /// Compiles mapping plans into executable mapper implementations.
    /// </summary>
    public interface IMappingGenerationBackend
    {
        /// <summary>
        /// Gets the backend name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Determines whether this backend supports the specified plan.
        /// </summary>
        /// <param name="plan">The mapping plan.</param>
        /// <returns>True when the plan is supported; otherwise, false.</returns>
        bool Supports(MappingPlan plan);

        /// <summary>
        /// Compiles the specified plan.
        /// </summary>
        /// <param name="plan">The mapping plan.</param>
        /// <returns>The compiled map.</returns>
        CompiledMap Compile(MappingPlan plan);
    }
}
