namespace OctoMap
{
    /// <summary>
    /// Builds OctoMap configuration from profiles.
    /// </summary>
    public interface IOctoMapConfigurationBuilder
    {
        /// <summary>
        /// Creates a map between the source and destination types.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <returns>The map expression for further configuration.</returns>
        IMapExpression<TSource, TDestination> CreateMap<TSource, TDestination>();

        /// <summary>
        /// Creates an explicitly configured multi-source map to the destination type.
        /// </summary>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <returns>The multi-source map expression for further configuration.</returns>
        IMultiMapExpression<TDestination> CreateMultiMap<TDestination>();

        /// <summary>
        /// Creates a map between the source and destination types.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        void CreateMap(Type sourceType, Type destinationType);

        /// <summary>
        /// Creates a projectable global type conversion.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="conversionExpression">The conversion expression.</param>
        void CreateConverter<TSource, TDestination>(System.Linq.Expressions.Expression<Func<TSource, TDestination>> conversionExpression);

        /// <summary>
        /// Creates a runtime global type conversion backed by a DI value converter.
        /// </summary>
        /// <typeparam name="TConverter">The converter type.</typeparam>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        void CreateConverter<TConverter, TSource, TDestination>()
            where TConverter : IValueConverter<TSource, TDestination>;

        /// <summary>
        /// Configures the source member naming convention for convention maps.
        /// </summary>
        /// <param name="namingConvention">The source naming convention.</param>
        void UseSourceNamingConvention(OctoMap.Naming.INamingConvention namingConvention);

        /// <summary>
        /// Configures the destination member naming convention for convention maps.
        /// </summary>
        /// <param name="namingConvention">The destination naming convention.</param>
        void UseDestinationNamingConvention(OctoMap.Naming.INamingConvention namingConvention);

        /// <summary>
        /// Removes source member prefixes before naming convention normalization.
        /// </summary>
        /// <param name="prefixes">The prefixes to remove.</param>
        void RecognizeSourcePrefixes(params string[] prefixes);

        /// <summary>
        /// Removes source member suffixes before naming convention normalization.
        /// </summary>
        /// <param name="suffixes">The suffixes to remove.</param>
        void RecognizeSourceSuffixes(params string[] suffixes);

        /// <summary>
        /// Removes destination member prefixes before naming convention normalization.
        /// </summary>
        /// <param name="prefixes">The prefixes to remove.</param>
        void RecognizeDestinationPrefixes(params string[] prefixes);

        /// <summary>
        /// Removes destination member suffixes before naming convention normalization.
        /// </summary>
        /// <param name="suffixes">The suffixes to remove.</param>
        void RecognizeDestinationSuffixes(params string[] suffixes);
    }
}
