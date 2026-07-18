using OctoMap.Planning;

namespace OctoMap
{
    /// <summary>
    /// Describes mapping plans in a human-readable format.
    /// </summary>
    public interface IMappingPlanDescriber
    {
        /// <summary>
        /// Describes the specified mapping plan.
        /// </summary>
        /// <param name="plan">The mapping plan.</param>
        /// <returns>The human-readable plan description.</returns>
        string Describe(MappingPlan plan);
    }
}
