namespace OctoMap
{
    /// <summary>
    /// Provides runtime object mapping operations.
    /// </summary>
    public interface IOctoMapper
    {
        /// <summary>
        /// Gets the projection builder used by queryable projection extensions.
        /// </summary>
        IOctoProjectionBuilder ProjectionBuilder { get; }

        /// <summary>
        /// Maps the specified source instance to a destination type.
        /// </summary>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source instance.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination Map<TDestination>(object source);

        /// <summary>
        /// Maps the specified source instance to a destination type.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source instance.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination Map<TSource, TDestination>(TSource source);

        /// <summary>
        /// Maps the specified source instances to a destination type using an explicitly configured multi-source map.
        /// </summary>
        /// <typeparam name="TSource1">The first source type.</typeparam>
        /// <typeparam name="TSource2">The second source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source1">The first source instance.</param>
        /// <param name="source2">The second source instance.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination Map<TSource1, TSource2, TDestination>(TSource1 source1, TSource2 source2);

        /// <summary>
        /// Maps the specified source instances to a destination type using an explicitly configured multi-source map.
        /// </summary>
        TDestination Map<TSource1, TSource2, TSource3, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3);

        /// <summary>
        /// Maps the specified source instances to a destination type using an explicitly configured multi-source map.
        /// </summary>
        TDestination Map<TSource1, TSource2, TSource3, TSource4, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4);

        /// <summary>
        /// Maps the specified source instances to a destination type using an explicitly configured multi-source map.
        /// </summary>
        TDestination Map<TSource1, TSource2, TSource3, TSource4, TSource5, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5);

        /// <summary>
        /// Maps the specified source instances to a destination type using an explicitly configured multi-source map.
        /// </summary>
        TDestination Map<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6);

        /// <summary>
        /// Maps the specified source instances to a destination type using an explicitly configured multi-source map.
        /// </summary>
        TDestination Map<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7);

        /// <summary>
        /// Maps the specified source instances to a destination type using an explicitly configured multi-source map.
        /// </summary>
        TDestination Map<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8);

        /// <summary>
        /// Maps the specified source instances to a destination type using an explicitly configured multi-source map.
        /// </summary>
        TDestination Map<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9);

        /// <summary>
        /// Maps the specified source instances to a destination type using an explicitly configured multi-source map.
        /// </summary>
        TDestination Map<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10);

        /// <summary>
        /// Maps the specified source instance onto an existing destination instance.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source instance.</param>
        /// <param name="destination">The existing destination instance.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination);

        /// <summary>
        /// Maps the specified source set to a destination type using an explicitly configured multi-source map.
        /// </summary>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="sources">The source set.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination Map<TDestination>(SourceSet sources);

        /// <summary>
        /// Compiles the specified single-source map without executing it.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        void CompileMap<TSource, TDestination>();

        /// <summary>
        /// Compiles the specified two-source map without executing it.
        /// </summary>
        /// <typeparam name="TSource1">The first source type.</typeparam>
        /// <typeparam name="TSource2">The second source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        void CompileMap<TSource1, TSource2, TDestination>();

        /// <summary>
        /// Compiles the specified three-source map without executing it.
        /// </summary>
        void CompileMap<TSource1, TSource2, TSource3, TDestination>();

        /// <summary>
        /// Compiles the specified four-source map without executing it.
        /// </summary>
        void CompileMap<TSource1, TSource2, TSource3, TSource4, TDestination>();

        /// <summary>
        /// Compiles the specified five-source map without executing it.
        /// </summary>
        void CompileMap<TSource1, TSource2, TSource3, TSource4, TSource5, TDestination>();

        /// <summary>
        /// Compiles the specified six-source map without executing it.
        /// </summary>
        void CompileMap<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TDestination>();

        /// <summary>
        /// Compiles the specified seven-source map without executing it.
        /// </summary>
        void CompileMap<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TDestination>();

        /// <summary>
        /// Compiles the specified eight-source map without executing it.
        /// </summary>
        void CompileMap<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TDestination>();

        /// <summary>
        /// Compiles the specified nine-source map without executing it.
        /// </summary>
        void CompileMap<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TDestination>();

        /// <summary>
        /// Compiles the specified ten-source map without executing it.
        /// </summary>
        void CompileMap<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TDestination>();

        /// <summary>
        /// Compiles the specified multi-source map without executing it.
        /// </summary>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="sourceTypes">The source types.</param>
        void CompileMap<TDestination>(params Type[] sourceTypes);

        /// <summary>
        /// Compiles all explicitly configured maps without executing them.
        /// </summary>
        void CompileMappings();
    }
}
