using System.Linq.Expressions;

namespace OctoMap
{
    /// <summary>
    /// Builds projection expressions from OctoMap configuration.
    /// </summary>
    public interface IOctoProjectionBuilder
    {
        /// <summary>
        /// Builds a projection expression for the specified source and destination types.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <returns>The projection expression.</returns>
        Expression<Func<TSource, TDestination>> Build<TSource, TDestination>();
    }
}
