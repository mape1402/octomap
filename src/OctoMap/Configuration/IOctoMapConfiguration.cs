namespace OctoMap
{
    /// <summary>
    /// Represents immutable OctoMap configuration.
    /// </summary>
    public interface IOctoMapConfiguration
    {
        /// <summary>
        /// Finds a configured type map.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>The matching type map when configured; otherwise, null.</returns>
        ITypeMap FindMap(Type sourceType, Type destinationType);

        /// <summary>
        /// Finds a configured multi-source type map.
        /// </summary>
        /// <param name="sourceTypes">The source types.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>The matching type map when configured; otherwise, null.</returns>
        ITypeMap FindMap(IReadOnlyList<Type> sourceTypes, Type destinationType);

        /// <summary>
        /// Gets the executable mapping plan for the specified source and destination types.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <returns>The mapping plan.</returns>
        OctoMap.Planning.MappingPlan GetPlan<TSource, TDestination>();

        /// <summary>
        /// Gets the executable mapping plan for the specified source and destination types.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>The mapping plan.</returns>
        OctoMap.Planning.MappingPlan GetPlan(Type sourceType, Type destinationType);

        /// <summary>
        /// Gets the executable mapping plan for the specified source types and destination type.
        /// </summary>
        /// <param name="sourceTypes">The source types.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>The mapping plan.</returns>
        OctoMap.Planning.MappingPlan GetPlan(IReadOnlyList<Type> sourceTypes, Type destinationType);

        /// <summary>
        /// Describes the executable mapping plan for the specified source and destination types.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <returns>The mapping plan description.</returns>
        string DescribeMap<TSource, TDestination>();

        /// <summary>
        /// Describes the executable mapping plan for the specified source and destination types.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>The mapping plan description.</returns>
        string DescribeMap(Type sourceType, Type destinationType);

        /// <summary>
        /// Describes the executable mapping plan for the specified source types and destination type.
        /// </summary>
        /// <param name="sourceTypes">The source types.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>The mapping plan description.</returns>
        string DescribeMap(IReadOnlyList<Type> sourceTypes, Type destinationType);

        /// <summary>
        /// Validates the current configuration.
        /// </summary>
        /// <returns>The validation report.</returns>
        OctoMapValidationReport Validate();

        /// <summary>
        /// Throws when the current configuration is invalid.
        /// </summary>
        void AssertValid();
    }
}
