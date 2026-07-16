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
        /// <param name="hasConstantValue">A value indicating whether this assignment uses a constant value.</param>
        /// <param name="constantValue">The constant value.</param>
        /// <param name="hasNullSubstitute">A value indicating whether this assignment has a null substitute.</param>
        /// <param name="nullSubstitute">The null substitute value.</param>
        public MemberAssignmentPlan(
            PropertyInfo destinationProperty,
            PropertyInfo sourceProperty,
            LambdaExpression sourceExpression,
            bool hasConstantValue,
            object constantValue,
            bool hasNullSubstitute,
            object nullSubstitute)
        {
            DestinationProperty = destinationProperty ?? throw new ArgumentNullException(nameof(destinationProperty));
            SourceProperty = sourceProperty;
            SourceExpression = sourceExpression;
            HasConstantValue = hasConstantValue;
            ConstantValue = constantValue;
            HasNullSubstitute = hasNullSubstitute;
            NullSubstitute = nullSubstitute;
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
        /// Gets whether this assignment uses a constant value.
        /// </summary>
        public bool HasConstantValue { get; }

        /// <summary>
        /// Gets the constant value.
        /// </summary>
        public object ConstantValue { get; }

        /// <summary>
        /// Gets whether this assignment has a null substitute.
        /// </summary>
        public bool HasNullSubstitute { get; }

        /// <summary>
        /// Gets the null substitute value.
        /// </summary>
        public object NullSubstitute { get; }

        /// <summary>
        /// Gets the destination property.
        /// </summary>
        public PropertyInfo DestinationProperty { get; }
    }
}
