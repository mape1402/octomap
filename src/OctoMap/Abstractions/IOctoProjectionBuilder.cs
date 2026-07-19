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

        /// <summary>
        /// Builds a projection expression for the specified source and destination types using external parameters.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="parameters">The projection parameters.</param>
        /// <returns>The projection expression.</returns>
        Expression<Func<TSource, TDestination>> Build<TSource, TDestination>(object parameters);

        /// <summary>
        /// Builds a projection expression for the specified source and destination types.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>The projection expression.</returns>
        LambdaExpression Build(Type sourceType, Type destinationType);

        /// <summary>
        /// Builds a projection expression for the specified source and destination types using external parameters.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <param name="parameters">The projection parameters.</param>
        /// <returns>The projection expression.</returns>
        LambdaExpression Build(Type sourceType, Type destinationType, object parameters);
    }
}
