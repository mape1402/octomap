namespace OctoMap.Planning
{
    /// <summary>
    /// Builds executable mapping plans from type maps.
    /// </summary>
    public interface IMappingPlanBuilder
    {
        /// <summary>
        /// Builds a mapping plan for the specified type map.
        /// </summary>
        /// <param name="typeMap">The type map.</param>
        /// <returns>The mapping plan.</returns>
        MappingPlan Build(ITypeMap typeMap);
    }
}
