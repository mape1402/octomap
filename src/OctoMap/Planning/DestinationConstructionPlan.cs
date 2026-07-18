using System.Linq.Expressions;
using System.Reflection;

namespace OctoMap.Planning
{
    /// <summary>
    /// Describes how a destination object is created before member assignments are applied.
    /// </summary>
    public sealed class DestinationConstructionPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DestinationConstructionPlan"/> class.
        /// </summary>
        /// <param name="constructionExpression">The configured construction expression.</param>
        /// <param name="constructor">The convention-selected destination constructor.</param>
        /// <param name="parameters">The convention constructor parameter plans.</param>
        /// <param name="constructedMemberNames">The destination member names covered by construction.</param>
        public DestinationConstructionPlan(
            LambdaExpression constructionExpression,
            ConstructorInfo constructor,
            IReadOnlyList<ConstructorParameterPlan> parameters,
            IReadOnlySet<string> constructedMemberNames)
        {
            ConstructionExpression = constructionExpression;
            Constructor = constructor;
            Parameters = parameters ?? Array.Empty<ConstructorParameterPlan>();
            ConstructedMemberNames = constructedMemberNames ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the configured construction expression.
        /// </summary>
        public LambdaExpression ConstructionExpression { get; }

        /// <summary>
        /// Gets the convention-selected destination constructor.
        /// </summary>
        public ConstructorInfo Constructor { get; }

        /// <summary>
        /// Gets the convention constructor parameter plans.
        /// </summary>
        public IReadOnlyList<ConstructorParameterPlan> Parameters { get; }

        /// <summary>
        /// Gets the destination member names covered by construction.
        /// </summary>
        public IReadOnlySet<string> ConstructedMemberNames { get; }
    }

    /// <summary>
    /// Describes one convention constructor parameter assignment.
    /// </summary>
    public sealed class ConstructorParameterPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructorParameterPlan"/> class.
        /// </summary>
        /// <param name="parameter">The destination constructor parameter.</param>
        /// <param name="sourceProperty">The matched source property.</param>
        public ConstructorParameterPlan(ParameterInfo parameter, PropertyInfo sourceProperty)
        {
            Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            SourceProperty = sourceProperty ?? throw new ArgumentNullException(nameof(sourceProperty));
        }

        /// <summary>
        /// Gets the destination constructor parameter.
        /// </summary>
        public ParameterInfo Parameter { get; }

        /// <summary>
        /// Gets the matched source property.
        /// </summary>
        public PropertyInfo SourceProperty { get; }
    }
}
