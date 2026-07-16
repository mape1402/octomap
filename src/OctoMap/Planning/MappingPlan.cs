using System.Reflection;
using System.Linq.Expressions;

namespace OctoMap.Planning
{
    /// <summary>
    /// Represents a convention-based executable mapping plan.
    /// </summary>
    public sealed class MappingPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MappingPlan"/> class.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <param name="assignments">The member assignment plans.</param>
        public MappingPlan(Type sourceType, Type destinationType, IReadOnlyList<MemberAssignmentPlan> assignments)
        {
            SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
            DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
            Assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
        }

        /// <summary>
        /// Gets the source type.
        /// </summary>
        public Type SourceType { get; }

        /// <summary>
        /// Gets the destination type.
        /// </summary>
        public Type DestinationType { get; }

        /// <summary>
        /// Gets the member assignment plans.
        /// </summary>
        public IReadOnlyList<MemberAssignmentPlan> Assignments { get; }
    }

    /// <summary>
    /// Represents one destination member assignment.
    /// </summary>
    public sealed class MemberAssignmentPlan
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberAssignmentPlan"/> class.
        /// </summary>
        /// <param name="destinationProperty">The destination property.</param>
        /// <param name="sourceProperty">The source property.</param>
        /// <param name="sourceExpression">The source expression.</param>
        public MemberAssignmentPlan(PropertyInfo destinationProperty, PropertyInfo sourceProperty, LambdaExpression sourceExpression)
        {
            DestinationProperty = destinationProperty ?? throw new ArgumentNullException(nameof(destinationProperty));
            SourceProperty = sourceProperty;
            SourceExpression = sourceExpression;
        }

        /// <summary>
        /// Gets the source property.
        /// </summary>
        public PropertyInfo SourceProperty { get; }

        /// <summary>
        /// Gets the source expression.
        /// </summary>
        public LambdaExpression SourceExpression { get; }

        /// <summary>
        /// Gets the destination property.
        /// </summary>
        public PropertyInfo DestinationProperty { get; }
    }
}
